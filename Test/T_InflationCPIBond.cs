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
         private IList<BootstrapHelper<ZeroInflationTermStructure>> makeHelpers(
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
         public Period observationLag;
         public DayCounter dayCounter;

         public UKRPI ii;

         public RelinkableHandle<YieldTermStructure> yTS = new RelinkableHandle<YieldTermStructure>();
         public RelinkableHandle<ZeroInflationTermStructure> cpiTS = new RelinkableHandle<ZeroInflationTermStructure>();

         public CommonVars()
         {
            calendar = new UnitedKingdom();
            convention = BusinessDayConvention.ModifiedFollowing;
            Date today = new Date(25, Month.November, 2009);
            evaluationDate = calendar.adjust(today);
            Settings.setEvaluationDate(evaluationDate);
            dayCounter = new ActualActual();

            Date from = new Date(20, Month.July, 2007);
            Date to = new Date(20, Month.November, 2009);
            Schedule rpiSchedule =
                new MakeSchedule().from(from).to(to)
                .withTenor(new Period(1, TimeUnit.Months))
                .withCalendar(new UnitedKingdom())
                .withConvention(BusinessDayConvention.ModifiedFollowing)
                .value();

            bool interp = false;
            ii = new UKRPI(interp, cpiTS);

            double[] fixData = {
                206.1, 207.3, 208.0, 208.9, 209.7, 210.9,
                209.8, 211.4, 212.1, 214.0, 215.1, 216.8,
                216.5, 217.2, 218.4, 217.7, 216,
                212.9, 210.1, 211.4, 211.3, 211.5,
                212.8, 213.4, 213.4, 213.4, 214.4
            };
            for (int i = 0; i < fixData.Length; ++i)
            {
               ii.addFixing(rpiSchedule[i], fixData[i]);
            }

            yTS.linkTo(new FlatForward(evaluationDate, 0.05, dayCounter));

            // now build the zero inflation curve
            observationLag = new Period(2, TimeUnit.Months);

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

            var helpers = makeHelpers(zciisData, ii, observationLag,
                                      calendar, convention, dayCounter);

            double baseZeroRate = zciisData[0].rate / 100.0;
            cpiTS.linkTo(new PiecewiseZeroInflationCurve<Linear>(
                         evaluationDate, calendar, dayCounter, observationLag,
                         ii.frequency(), ii.interpolated(), baseZeroRate,
                         new Handle<YieldTermStructure>(yTS), helpers.ToList()));
         }
      }

      [TestMethod()]
      public void testCleanPrice()
      {
         CommonVars common = new CommonVars();
         double notional = 1000000.0;
         List<double> fixedRates = new List<double>() { 0.1 };
         DayCounter fixedDayCount = new Actual365Fixed();
         BusinessDayConvention fixedPaymentConvention = BusinessDayConvention.ModifiedFollowing;
         Calendar fixedPaymentCalendar = new UnitedKingdom();
         ZeroInflationIndex fixedIndex = common.ii;
         Period contractObservationLag = new Period(3, TimeUnit.Months);
         InterpolationType observationInterpolation = InterpolationType.Flat;
         int settlementDays = 3;
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

         CPIBond bond = new CPIBond(settlementDays, notional, growthOnly,
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
   }
}
