﻿using APSIM.Cloud.Shared;
using APSIM.Shared.Soils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace UnitTestProject1
{
    [TestClass]
    public class YPToAPSIMSpecTests
    {

        [TestMethod]
        public void YPToAPSIMSpecTests_EnsureYPToAPSIMWorks()
        {
            Sample sample = new Sample();
            sample.Thickness = new double[] { 100, 300, 300, 300 };
            sample.NO3 = new double[] { 34, 6.9, 3.1, 1.8 };
            sample.NH4 = new double[] { 5.5, 1.8, 1.8, 1.5 };
            sample.SW = new double[] { 0.13, 0.18, 0.20, 0.24 };
            sample.SWUnits = Sample.SWUnitsEnum.Gravimetric;

            Sow sow = new Sow();
            sow.Crop = "Wheat";
            sow.Date = new DateTime(2016, 5, 1);

            Paddock paddock = new Paddock();
            paddock.Name = "NameOfPaddock";
            paddock.StartSeasonDate = new DateTime(2016, 4, 1);
            paddock.NowDate = new DateTime(2016, 7, 1);
            paddock.SoilWaterSampleDate = new DateTime(2016, 3, 1);
            paddock.SoilNitrogenSampleDate = new DateTime(2016, 6, 1);
            paddock.StationNumber = 41023;
            paddock.StationName = "Toowoomba";
            paddock.RunType = Paddock.RunTypeEnum.SingleSeason;
            paddock.StubbleMass = 100;
            paddock.StubbleType = "Wheat";
            paddock.Samples = new List<Sample>();
            paddock.Samples.Add(sample);
            paddock.SoilPath = "Soils/Australia/Victoria/Wimmera/Clay (Rupanyup North No742)";
            paddock.Management = new List<Management>();
            paddock.Management.Add(sow);

            YieldProphet yp = new YieldProphet();
            yp.Paddock = new List<Paddock>();
            yp.Paddock.Add(paddock);

            List<APSIMSpec> simulations = YieldProphetToAPSIM.ToAPSIM(yp);

            Assert.AreEqual(simulations.Count, 1);
            Assert.AreEqual(simulations[0].Name, "NameOfPaddock");
            Assert.AreEqual(simulations[0].StartDate, new DateTime(2016, 3, 1));
            Assert.AreEqual(simulations[0].EndDate, paddock.NowDate.AddDays(-1));
            Assert.AreEqual(simulations[0].Management.Count, 5);
            Assert.IsTrue(simulations[0].Management[0] is ResetWater);
            Assert.AreEqual(simulations[0].Management[0].Date, new DateTime(2016, 3, 1));
            Assert.IsTrue(simulations[0].Management[1] is ResetSurfaceOrganicMatter);
            Assert.AreEqual(simulations[0].Management[1].Date, new DateTime(2016, 3, 1));
            Assert.IsTrue(simulations[0].Management[2] is ResetNitrogen);
            Assert.AreEqual(simulations[0].Management[2].Date, new DateTime(2016, 5, 1));
            Assert.IsTrue(simulations[0].Management[3] is Sow);
            Assert.AreEqual(simulations[0].Management[3].Date, new DateTime(2016, 5, 1));
            Assert.IsTrue(simulations[0].Management[4] is ResetNitrogen);
            Assert.AreEqual(simulations[0].Management[4].Date, new DateTime(2016, 6, 1));
        }


    }
}