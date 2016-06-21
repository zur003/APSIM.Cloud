﻿// -----------------------------------------------------------------------
// <copyright file="ProcessYPJob.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace APSIM.Cloud.Runner.RunnableJobs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Xml;
    using System.Reflection;
    using System.Data;
    using System.Threading;
    using APSIM.Cloud.Shared;
    using APSIM.Cloud.Shared.AusFarm;
    using APSIM.Shared.Utilities;
    using APSIM.Shared.OldAPSIM;
    //using ApsimFile;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ProcessYPJob : JobManager.IRunnable
    {
        /// <summary>The working directory</summary>
        private string workingDirectory;

        /// <summary>The apsim report executable path.</summary>
        private static string archiveLocation = @"ftp://bob.apsim.info/APSIM.Cloud.Archive";

        /// <summary>Should this job run APSIM or just create all necessary files.</summary>
        private bool runAPSIM;

        /// <summary>Gets a value indicating whether this instance is computationally time consuming.</summary>
        public bool IsComputationallyTimeConsuming { get { return false; } }

        /// <summary>Gets or sets the error message. Set by the JobManager.</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Gets or sets a value indicating whether this job is completed. Set by the JobManager.</summary>
        public bool IsCompleted { get; set; }

        /// <summary>Gets or sets the name of the job. If null, JobFileName will be used.</summary>
        public string JobName { get; set; }

        /// <summary>The job file. If null then the JobName will be used to get the XML from the jobs db.</summary>
        public string JobFileName { get; set; }

        /// <summary>Optional path to APSIM.exe</summary>
        public string ApsimExecutable { get; set; }

        /// <summary>Constructor</summary>
        /// <param name="runAPSIM">Run APSIM?</param>
        public ProcessYPJob(bool runAPSIM)
        {
            this.runAPSIM = runAPSIM;
        }

        /// <summary>
        /// Runs the YP job.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.ComponentModel.DoWorkEventArgs"/> instance containing the event data.</param>
        public void Run(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            // Get our job manager.
            JobManager jobManager = (JobManager)e.Argument;

            // Create a working directory.
            workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(workingDirectory);
            string jobXML;

            if (JobFileName == null)
            {
                using (JobsService.JobsClient jobsClient = new JobsService.JobsClient())
                {
                    jobXML = jobsClient.GetJobXML(JobName);
                }
            }
            else
            {
                if (Path.GetExtension(JobFileName) == ".zip")
                    ZipUtilities.UnZipFiles(JobFileName, workingDirectory, null);
                else
                    File.Copy(JobFileName, Path.Combine(workingDirectory, Path.GetFileName(JobFileName)));
                string[] xmlFileNames = Directory.GetFiles(workingDirectory, "*.xml");
                if (xmlFileNames.Length == 0)
                    throw new Exception("Cannot find a job xml file");
                StreamReader reader = new StreamReader(xmlFileNames[0]);
                jobXML = reader.ReadToEnd();
                reader.Close();
                JobName = Path.GetFileNameWithoutExtension(xmlFileNames[0]);
            }

            // Create and run a job.
            try
            {
                JobManager.IRunnable job = CreateRunnableJob(JobName, jobXML, workingDirectory, ApsimExecutable);
                if (runAPSIM)
                {
                    jobManager.AddJob(job);
                    while (!job.IsCompleted)
                        Thread.Sleep(5 * 1000); // 5 sec
                }
                ErrorMessage = job.ErrorMessage;
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message + "\r\n" + err.StackTrace;
            }

            // Zip the temporary directory and send to archive.
            string zipFileName = Path.Combine(workingDirectory, JobName + ".zip");
            ZipUtilities.ZipFiles(Directory.GetFiles(workingDirectory), zipFileName, null);

            // Tell the job system that job is complete.
            if (JobFileName == null)
            {
                string pwdFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ftpuserpwd.txt");
                if (!File.Exists(pwdFile))
                    ErrorMessage = "Cannot find file: " + pwdFile;
                else
                {
                    string[] usernamepwd = File.ReadAllText(pwdFile).Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    FTPClient.Upload(zipFileName, archiveLocation + "/" + JobName + ".zip", usernamepwd[0], usernamepwd[1]);
                }
                using (JobsService.JobsClient jobsClient = new JobsService.JobsClient())
                {
                    if (ErrorMessage != null)
                        ErrorMessage = ErrorMessage.Replace("'", "");
                    jobsClient.SetCompleted(JobName, ErrorMessage);
                }
            }
            else
            {
                string destZipFileName = Path.ChangeExtension(JobFileName, ".out.zip");
                File.Copy(zipFileName, destZipFileName, true);
            }
            // Get rid of our temporary directory.
            Directory.Delete(workingDirectory, true);

            if (ErrorMessage != null)
                throw new Exception(ErrorMessage);
        }

        /// <summary>Create a runnable job for the simulations</summary>
        /// <param name="FilesToRun">The files to run.</param>
        /// <param name="ApsimExecutable">APSIM.exe path. Can be null.</param>
        /// <returns>A runnable job for all simulations</returns>
        private static JobManager.IRunnable CreateRunnableJob(string jobName, string jobXML, string workingDirectory, string ApsimExecutable)
        {
            // Create a sequential job.
            JobSequence completeJob = new JobSequence();
            completeJob.Jobs = new List<JobManager.IRunnable>();

            List<JobManager.IRunnable> jobs = new List<JobManager.IRunnable>();
            DateTime nowDate;

            // Determine if this is a YP or a F4P job to be added to the job list
            if (jobName.IndexOf("_F4P") > 0)
            {
                // Save YieldProphet.xml to working folder.
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(jobXML);
                doc.Save(Path.Combine(workingDirectory, "f4p.xml"));

                Farm4Prophet spec = Farm4ProphetUtility.Farm4ProphetFromXML(jobXML);
                List<AusFarmSpec> simulations = Farm4ProphetToAusFarm.ToAusFarm(spec);

                //writes the sdml files to the workingDirectory and returns a list of names
                string[] files = AusFarmFiles.Create(simulations, workingDirectory);

                string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                //for each simulation
                for (int i = 0; i < files.Length; i++)
                {
                    RunnableJobs.AusFarmJob job = new RunnableJobs.AusFarmJob(
                     jobName: jobName,
                     fileName: Path.Combine(workingDirectory, files[i]),
                     arguments: "");
                    jobs.Add(job);
                }
                nowDate = DateTime.Now;

                completeJob.Jobs.Add(new JobParallel() { Jobs = jobs });
            }
            else
            {
                // Create a YieldProphet object from our YP xml file
                YieldProphet spec = YieldProphetUtility.YieldProphetFromXML(jobXML, workingDirectory);

                string fileBaseToWrite;
                if (spec.ReportType == YieldProphet.ReportTypeEnum.None && spec.ReportName != null)
                    fileBaseToWrite = spec.ReportName;
                else
                    fileBaseToWrite = "YieldProphet";

                // Convert YieldProphet spec into a simulation set.
                List<APSIMSpec> simulations = YieldProphetToAPSIM.ToAPSIM(spec);

                // Create all the files needed to run APSIM.
                string apsimFileName = APSIMFiles.Create(simulations, workingDirectory, fileBaseToWrite + ".apsim");

                // Fill in calculated fields.
                foreach (Paddock paddock in spec.Paddock)
                    YieldProphetUtility.FillInCalculatedFields(paddock, paddock.ObservedData, workingDirectory);

                // Save YieldProphet.xml to working folder.
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(YieldProphetUtility.YieldProphetToXML(spec));
                doc.Save(Path.Combine(workingDirectory, fileBaseToWrite + ".xml"));

                // Convert .apsim file to sims so that we can run ApsimModel.exe rather than Apsim.exe
                // This will avoid using the old APSIM job runner. It assumes though that there are 
                // no other APSIMJob instances running in the workingDirectory. This is because it
                // looks and runs all .sim files it finds in the workingDirectory.
                JobParallel simJobs = new JobParallel();
                simJobs.Jobs = new List<JobManager.IRunnable>();
                string[] simFileNames = CreateSimFiles(apsimFileName, workingDirectory);
                foreach (string simFileName in simFileNames)
                    simJobs.Jobs.Add(new RunnableJobs.APSIMJob(simFileName, workingDirectory, ApsimExecutable, true));
                completeJob.Jobs.Add(simJobs);
                completeJob.Jobs.Add(new RunnableJobs.APSIMPostSimulationJob(workingDirectory));
                if (spec.Paddock.Count > 0 && spec.Paddock[0].RunType != Paddock.RunTypeEnum.Validation)
                    completeJob.Jobs.Add(new RunnableJobs.YPPostSimulationJob(jobName, spec.Paddock[0].NowDate, workingDirectory));
            }
            return completeJob;
        }

        /// <summary>Convert .apsim file to .sim files</summary>
        /// <param name="apsimFileName">The .apsim file name.</param>
        /// <param name="workingDirectory">The working directory</param>
        /// <returns>A list of filenames for all created .sim files.</returns>
        private static string[] CreateSimFiles(string apsimFileName, string workingDirectory)
        {
            string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string executable = Path.Combine(binDirectory, @"APSIM\Model\ApsimToSim.exe");

            RunnableJobs.APSIMJob job = new RunnableJobs.APSIMJob(apsimFileName, workingDirectory, executable);
            job.Run(null, null);
            return Directory.GetFiles(workingDirectory, "*.sim");
        }
    }
}
