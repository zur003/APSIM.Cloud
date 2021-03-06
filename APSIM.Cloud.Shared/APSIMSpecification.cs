﻿// -----------------------------------------------------------------------
// <copyright file="APSIMSpecification.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace APSIM.Cloud.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Xml.Serialization;

    /// <summary>
    /// A specification for an APSIM simulation.
    /// </summary>
    public class APSIMSpecification
    {
        /// <summary>Gets or sets the name of the simulation</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the start date of the simulation</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Gets or sets the end date of the simulation</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Gets or sets the first year for long term simulations</summary>
        public int LongtermStartYear { get; set; }

        /// <summary>
        /// Gets or sets the 'now' date. When creating weather files anything after this
        /// date will assumed to be in the future so historical weather data will be used
        /// after this date.
        /// </summary>
        public DateTime NowDate { get; set; }

        /// <summary>Gets or sets the SILO station number.</summary>
        public int StationNumber { get; set; }

        /// <summary>Gets or sets any observed data. Can be null if no data.</summary>
        public DataTable ObservedData { get; set; }

        /// <summary>Gets or sets the name of the weather file.</summary>
        public string WeatherFileName { get; set; }


        /// <summary>Type of APSIM run to create.</summary>
        public enum RunTypeEnum
        {
            /// <summary>Run just this season.</summary>
            Normal,

            /// <summary>Run a series of longterm simulations, patched with data from this year. Results will be combined.</summary>
            LongTermPatched,
        }

        /// <summary>Type of run to perform.</summary>
        public RunTypeEnum RunType { get; set; }

        /// <summary>Gets or sets the stubble mass (kg/ha)</summary>
        public double StubbleMass { get; set; }

        /// <summary>Gets or sets the type of the stubble.</summary>
        public string StubbleType { get; set; }

        /// <summary>Gets or sets the slope (%).</summary>
        public double Slope { get; set; }

        /// <summary>Gets or sets the slope length (m).</summary>
        public double SlopeLength { get; set; }

        /// <summary>Gets or sets a value indicating whether EC and CL should be used.</summary>
        public bool UseEC { get; set; }

        /// <summary>Gets or sets the list of management actions.</summary>
        [XmlArrayItem(typeof(Sow))]
        [XmlArrayItem(typeof(Fertilise))]
        [XmlArrayItem(typeof(Irrigate))]
        [XmlArrayItem(typeof(Tillage))]
        [XmlArrayItem(typeof(StubbleRemoved))]
        [XmlArrayItem(typeof(ResetWater))]
        [XmlArrayItem(typeof(ResetNitrogen))]
        [XmlArrayItem(typeof(ResetSurfaceOrganicMatter))]
        public List<Management> Management { get; set; }

        /// <summary>Gets or sets the soil samples.</summary>
        public List<APSIM.Shared.Soils.Sample> Samples { get; set; }

        /// <summary>Gets or sets the full APSoil path of the soil to use to lookup APSoil.</summary>
        public string SoilPath { get; set; }

        /// <summary>Gets or sets the soil. If null, the 'SoilName' property will
        /// be used to lookup the soil from APSoil</summary>
        public APSIM.Shared.Soils.Soil Soil { get; set; }

        /// <summary>Gets or sets the total water (mm) at the beginning of the simulation.</summary>
        public double InitTotalWater { get; set; }

        /// <summary>Gets or sets the total nitrogen (kg/ha) at the beginning of the simulation.</summary>
        public double InitTotalNitrogen { get; set; }

        /// <summary>Gets or sets a value indicating whether the run should be nitrogen unlimited</summary>
        public bool NUnlimited { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the run should be nitrogen 
        /// unlimited from today until the end of the crop.
        /// </summary>
        public bool NUnlimitedFromToday { get; set; }

        /// <summary>Gets or sets a value indicating whether daily output is required.</summary>
        public bool DailyOutput { get; set; }

        /// <summary>Gets or sets a value indicating whether monthly output is required.</summary>
        public bool MonthlyOutput { get; set; }

        /// <summary>Gets or sets a value indicating whether yearly output is required.</summary>
        public bool YearlyOutput { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a depth.out file should be created at
        /// the end of the simulation run.
        /// </summary>
        public bool WriteDepthFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a 'next 10 days' output file should be created.
        /// </summary>
        public bool Next10DaysDry { get; set; }

        /// <summary>Gets or sets the date where the decile file starts from.</summary>
        public DateTime DecileDate { get; set; }

        ////////////// The below fields are auto-generated //////////////

        /// <summary>Gets or sets the factors for factorial runs.</summary>
        public List<Factor> Factors { get; set; }

        /// <summary>Errors that occurred during creation of the simulation. Null if no error.</summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// A factor
        /// </summary>
        public struct Factor
        {
            public string Name;
            public string ComponentPath;
            public string ComponentVariableName;
            public string[] ComponentVariableValues;
        }
    }
}
