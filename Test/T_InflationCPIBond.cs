/*
 Copyright (C) 2015 Francois Botha (igitur@gmail.com)

 This file is part of QLNet Project http://qlnet.sourceforge.net/

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available online at <http://qlnet.sourceforge.net/License.html>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestSuite
{
   [TestClass()]
   public class T_InflationCPIBond
   {
      private class CommonVars
      {
         private static IList<BootstrapHelper<ZeroInflationTermStructure>> makeHelpers(
            Datum[] iiData,
            ZeroInflationIndex ii,
            Period observationLag,
            Calendar calendar,
            BusinessDayConvention bdc,
            DayCounter dc)
         {
            IList<BootstrapHelper<ZeroInflationTermStructure>> instruments = new List<BootstrapHelper<ZeroInflationTermStructure>>();
            for (int i = 0; i < iiData.Length; i++)
            {
               Date maturity = iiData[i].date;
               Handle<Quote> quote = new Handle<Quote>(new SimpleQuote(iiData[i].rate / 100.0));
               BootstrapHelper<ZeroInflationTermStructure> h = new ZeroCouponInflationSwapHelper(quote, observationLag,
                                                                                                 maturity, calendar,
                                                                                                 bdc, dc, ii);
               instruments.Add(h);
            }
            return instruments;
         }

         // common data
         public Calendar calendar;

         public BusinessDayConvention convention;
         public Date evaluationDate;
         public int settlementDays;
         public Date settlementDate;
         public Period observationLag;
         public DayCounter dayCounter;

         public ZeroInflationIndex ii;

         public RelinkableHandle<YieldTermStructure> yTS = new RelinkableHandle<YieldTermStructure>();
         public RelinkableHandle<ZeroInflationTermStructure> cpiTS = new RelinkableHandle<ZeroInflationTermStructure>();


         public CommonVars() { }

         public static CommonVars UKVars()
         {
            var uk = new CommonVars();
            uk.calendar = new UnitedKingdom();
            uk.convention = BusinessDayConvention.ModifiedFollowing;
            Date today = new Date(25, Month.November, 2009);
            uk.evaluationDate = uk.calendar.adjust(today);
            uk.settlementDays = 3;
            uk.dayCounter = new ActualActual();

            Date from = new Date(20, Month.July, 2007);
            Date to = new Date(20, Month.November, 2009);
            Schedule rpiSchedule =
                new MakeSchedule().from(from).to(to)
                .withTenor(new Period(1, TimeUnit.Months))
                .withCalendar(new UnitedKingdom())
                .withConvention(BusinessDayConvention.ModifiedFollowing)
                .value();

            bool interp = false;
            uk.ii = new UKRPI(interp, uk.cpiTS);

            double[] fixData = {
                206.1, 207.3, 208.0, 208.9, 209.7, 210.9,
                209.8, 211.4, 212.1, 214.0, 215.1, 216.8,
                216.5, 217.2, 218.4, 217.7, 216,
                212.9, 210.1, 211.4, 211.3, 211.5,
                212.8, 213.4, 213.4, 213.4, 214.4
            };
            for (int i = 0; i < fixData.Length; ++i)
            {
               uk.ii.addFixing(rpiSchedule[i], fixData[i]);
            }

            uk.yTS.linkTo(new FlatForward(uk.evaluationDate, 0.05, uk.dayCounter));

            // now build the zero inflation curve
            uk.observationLag = new Period(2, TimeUnit.Months);

            Datum[] zciisData = {
                new Datum( new Date(25, Month.November, 2010), 3.0495 ),
                new Datum(new Date(25, Month.November, 2011), 2.93 ),
                new Datum( new Date(26, Month.November, 2012), 2.9795 ),
                new Datum(  new Date(25, Month.November, 2013), 3.029 ),
                new Datum( new Date(25, Month.November, 2014), 3.1425 ),
                new Datum( new Date(25, Month.November, 2015), 3.211 ),
                new Datum( new Date(25, Month.November, 2016), 3.2675 ),
                new Datum( new Date(25, Month.November, 2017), 3.3625 ),
                new Datum( new Date(25, Month.November, 2018), 3.405 ),
                new Datum( new Date(25, Month.November, 2019), 3.48 ),
                new Datum( new Date(25, Month.November, 2021), 3.576 ),
                new Datum( new Date(25, Month.November, 2024), 3.649 ),
                new Datum( new Date(26, Month.November, 2029), 3.751 ),
                new Datum( new Date(27, Month.November, 2034), 3.77225 ),
                new Datum( new Date(25, Month.November, 2039), 3.77 ),
                new Datum( new Date(25, Month.November, 2049), 3.734 ),
                new Datum( new Date(25, Month.November, 2059), 3.714 )
            };

            var helpers = makeHelpers(zciisData, uk.ii, uk.observationLag,
                                      uk.calendar, uk.convention, uk.dayCounter);

            double baseZeroRate = zciisData[0].rate / 100.0;
            uk.cpiTS.linkTo(new PiecewiseZeroInflationCurve<Linear>(
                         uk.evaluationDate, uk.calendar, uk.dayCounter, uk.observationLag,
                         uk.ii.frequency(), uk.ii.interpolated(), baseZeroRate,
                         new Handle<YieldTermStructure>(uk.yTS), helpers.ToList()));

            return uk;

         }
         public static CommonVars ZAVars()
         {
            var za = new CommonVars();
            za.calendar = new NullCalendar();
            za.convention = BusinessDayConvention.ModifiedFollowing;
            var today = new Date(31, Month.July, 2015);
            za.settlementDate = new Date(5, Month.Aug, 2015);
            za.evaluationDate = za.calendar.adjust(today);
            za.dayCounter = new ActualActual();
            za.observationLag = new Period(4, TimeUnit.Months);

            //When calculating prices from YTM, we have to use NullCalendar, but settlementDate is still based on actual SouthAfrica() calendar
            za.settlementDays = za.calendar.businessDaysBetween(za.evaluationDate, za.settlementDate);

            bool interp = true;
            int indexAvailability = 1;
            int cpiIndexLag = -4;

            //za.ii = new ZACPI(interp, za.cpiTS);
            za.ii = new ZeroInflationIndex("CPI", new ZARegion(), false, interp, Frequency.Monthly, new Period(indexAvailability, TimeUnit.Months), new ZARCurrency(), za.cpiTS, true);

            var cpiFixings = new Dictionary<DateTime, double>()
            {
               { new DateTime(2015,1,31), 110.8},
               { new DateTime(2015,2,28), 111.5},
               { new DateTime(2015,3,31), 113.1},
               { new DateTime(2015,4,30), 114.1},
               { new DateTime(2015,5,31), 114.4},
               { new DateTime(2015,6,30), 114.9}
            };
            DateTime expectedCPIEndDate = za.calendar.endOfMonth(za.calendar.advance(za.settlementDate, cpiIndexLag + 1, TimeUnit.Months, za.convention));
            foreach (var fixing in cpiFixings.Where(f => f.Key <= expectedCPIEndDate))
            {
               za.ii.addFixing(fixing.Key, fixing.Value * 1.267);
            }

            za.yTS.linkTo(new FlatForward(za.evaluationDate, 0.00, za.dayCounter));

            Dictionary<Date, double> futureInflationIncreases = new Dictionary<Date, double>() {
                    { za.calendar.advance(za.settlementDate, new Period(cpiIndexLag, TimeUnit.Months)), 0},
                    { new Date(1, Month.May, 2050), 0}
                };

            var izic = new InterpolatedZeroInflationCurve<Linear>(
                         za.settlementDate, za.calendar, new Actual365Fixed(), za.observationLag,
                         za.ii.frequency(), true, za.yTS, futureInflationIncreases.Keys.ToList(), futureInflationIncreases.Values.ToList());
            za.cpiTS.linkTo(izic);

            return za;
         }

      }

      [TestMethod()]
      public void testCleanPrice()
      {
         CommonVars common = CommonVars.UKVars();
         Settings.setEvaluationDate(common.evaluationDate);

         double notional = 1000000.0;
         List<double> fixedRates = new List<double>() { 0.1 };
         DayCounter fixedDayCount = new Actual365Fixed();
         BusinessDayConvention fixedPaymentConvention = BusinessDayConvention.ModifiedFollowing;
         Calendar fixedPaymentCalendar = new UnitedKingdom();
         ZeroInflationIndex fixedIndex = common.ii;
         Period contractObservationLag = new Period(3, TimeUnit.Months);
         InterpolationType observationInterpolation = InterpolationType.Flat;
         common.settlementDays = 3;
         common.settlementDate = common.calendar.advance(common.evaluationDate, common.settlementDays, TimeUnit.Days);

         bool growthOnly = true;

         double baseCPI = 206.1;
         // set the schedules
         Date startDate = new Date(2, Month.October, 2007);
         Date endDate = new Date(2, Month.October, 2052);
         Schedule fixedSchedule =
             new MakeSchedule().from(startDate).to(endDate)
                           .withTenor(new Period(6, TimeUnit.Months))
                           .withCalendar(new UnitedKingdom())
                           .withConvention(BusinessDayConvention.Unadjusted)
                           .backwards()
                           .value();

         CPIBond bond = new CPIBond(common.settlementDays, notional, growthOnly,
                                    baseCPI, contractObservationLag, fixedIndex,
                                    observationInterpolation, fixedSchedule,
                                    fixedRates, fixedDayCount, fixedPaymentConvention);

         DiscountingBondEngine engine = new DiscountingBondEngine(common.yTS);
         bond.setPricingEngine(engine);

         double storedPrice = 383.01816406;
         double calculated = bond.cleanPrice();
         double tolerance = 1.0e-8;
         if (Math.Abs(storedPrice - calculated) > tolerance)
         {
            Assert.Fail("failed to reproduce expected CPI-bond clean price"
                + "\n    expected:    " + storedPrice
                + "\n    calculated': " + calculated
                + "\n    error':      " + (storedPrice - calculated));
         }
      }

      private struct test_case
      {
         public string code;
         public Date issueDate;
         public Date maturityDate;
         public double couponRate;
         public double ytm;
         public double baseCPI;
         public double dirtyPrice;
      }

      [TestMethod()]
      public void testZABondsReferencePeriod()
      {
         // Test ZA CPI bonds' dirty prices on 2015-07-31 using their quoted yields to maturity
         CommonVars common = CommonVars.ZAVars();
         Settings.setEvaluationDate(common.evaluationDate);

         double notional = 100;

         DayCounter bondDayCounter = new ActualActual(ActualActual.Convention.Bond);
         BusinessDayConvention paymentConvention = BusinessDayConvention.ModifiedFollowing;
         ZeroInflationIndex cpiIndices = common.ii;
         Period observationLag = new Period(4, TimeUnit.Months);
         InterpolationType observationInterpolation = InterpolationType.Linear;
         bool growthOnly = false;

         IList<test_case> cases = new List<test_case>()
         {
            { new test_case() { code = "R211",  issueDate = new Date(09, Month.Jun, 2010), maturityDate = new Date(31, Month.Jan, 2017), couponRate = 0.0250, ytm=0.00825, baseCPI=110.44,      dirtyPrice=134.22128 } },
            { new test_case() { code = "R212",  issueDate = new Date(17, Month.Jun, 2010), maturityDate = new Date(31, Month.Jan, 2022), couponRate = 0.0275, ytm=0.01445, baseCPI=110.68,      dirtyPrice=141.22928 } },
            { new test_case() { code = "R197",  issueDate = new Date(30, Month.May, 2001), maturityDate = new Date(07, Month.Dec, 2023), couponRate = 0.0550, ytm=0.01525, baseCPI=65.05040323, dirtyPrice=293.22573 } },
            { new test_case() { code = "I2025", issueDate = new Date(04, Month.Jul, 2012), maturityDate = new Date(31, Month.Jan, 2025), couponRate = 0.0200, ytm=0.01515, baseCPI=122.6483871, dirtyPrice=122.97651 } },
            { new test_case() { code = "R210",  issueDate = new Date(27, Month.Sep, 2007), maturityDate = new Date(31, Month.Mar, 2028), couponRate = 0.0260, ytm=0.01625, baseCPI=89.275,      dirtyPrice=181.44388 } },
            { new test_case() { code = "I2033", issueDate = new Date(20, Month.Aug, 2003), maturityDate = new Date(07, Month.Dec, 2033), couponRate = 0.0345, ytm=0.01780, baseCPI=76.82258065, dirtyPrice=238.29050 } },
            { new test_case() { code = "I2038", issueDate = new Date(04, Month.Jul, 2012), maturityDate = new Date(31, Month.Jan, 2038), couponRate = 0.0225, ytm=0.01810, baseCPI=122.6483871, dirtyPrice=127.49386 } },
            { new test_case() { code = "I2046", issueDate = new Date(17, Month.Jul, 2013), maturityDate = new Date(31, Month.Mar, 2046), couponRate = 0.0250, ytm=0.01950, baseCPI=130.1290742, dirtyPrice=126.14694 } },
            { new test_case() { code = "I2050", issueDate = new Date(11, Month.Jul, 2012), maturityDate = new Date(31, Month.Dec, 2050), couponRate = 0.0250, ytm=0.01925, baseCPI=122.7612903, dirtyPrice=135.41703 } }
         };

         foreach (var c in cases)
         {
            // set the schedules
            Schedule schedule = new Schedule(
                c.issueDate,
                c.maturityDate,
                new Period(12 / 2, TimeUnit.Months),
                common.calendar,
                BusinessDayConvention.Unadjusted,
                BusinessDayConvention.Unadjusted,
                DateGeneration.Rule.Backward,
                false);

            CPIBond bond = new CPIBond(common.settlementDays, notional, growthOnly,
                                       c.baseCPI, observationLag, cpiIndices,
                                       observationInterpolation, schedule,
                                       new List<double>() { c.couponRate }, bondDayCounter, paymentConvention,
                                       null, schedule.calendar(),
                                       //Ex-coupon parameters
                                       new Period(10, TimeUnit.Days),
                                       new NullCalendar(),
                                       BusinessDayConvention.Unadjusted,
                                       false);

            DiscountingBondEngine engine = new DiscountingBondEngine(common.yTS);
            bond.setPricingEngine(engine);

            double calculated = BondFunctions.dirtyPrice(
               bond,
               new InterestRate(c.ytm, new ActualActual(ActualActual.Convention.Bond), Compounding.Compounded, Frequency.Semiannual),
               common.settlementDate);

            double tolerance = 2.0e-5;
            if (Math.Abs(c.dirtyPrice - calculated) > tolerance)
            {
               Assert.Fail("failed to reproduce expected CPI-bond clean price"
                   + "\n    expected:    " + c.dirtyPrice
                   + "\n    calculated': " + calculated
                   + "\n    error':      " + (c.dirtyPrice - calculated));
            }
         }
      }
   }
}
