//MIT License
// BioticIndicesCalculator.cs - Helper class for calculating various biotic indices for foraminiferal assemblages

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ForamEcoQS
{
    /// <summary>
    /// Calculator class for various foraminiferal biotic indices
    /// References:
    /// - exp(H'bc): Chao & Shen (2003), Bouchet et al. (2012)
    /// - FSI: Dimiza et al. (2016)
    /// - TSI-Med: Barras et al. (2014)
    /// - NQIf: Alve et al. (2019)
    /// - H'log2: Shannon & Weaver (1963)
    /// - FIEI: Mojtahid et al. (2006), Denoyelle et al. (2010)
    /// - FoRAM Index: Hallock et al. (2003), Prazeres et al. (2020)
    /// - BENTIX: Simboura & Zenetos (2002)
    /// - BQI: Rosenberg et al. (2004), adapted for foraminifera
    /// </summary>
    public class BioticIndicesCalculator
    {
        #region Shannon Diversity Indices

        /// <summary>
        /// Calculates the Shannon-Wiener index using natural logarithm (H')
        /// H' = -Î£(pi Ã— ln(pi))
        /// </summary>
        public static double CalculateShannonLn(double[] abundances)
        {
            double total = abundances.Sum();
            if (total == 0) return 0;

            double h = 0;
            foreach (double n in abundances)
            {
                if (n > 0)
                {
                    double p = n / total;
                    h -= p * Math.Log(p);
                }
            }
            return Math.Round(h, 4);
        }

        /// <summary>
        /// Calculates the Shannon-Wiener index using log base 2 (H'log2)
        /// H'log2 = -Î£(pi Ã— log2(pi))
        /// Reference: Shannon & Weaver (1963)
        /// </summary>
        public static double CalculateShannonLog2(double[] abundances)
        {
            double total = abundances.Sum();
            if (total == 0) return 0;

            double h = 0;
            foreach (double n in abundances)
            {
                if (n > 0)
                {
                    double p = n / total;
                    h -= p * Math.Log2(p);
                }
            }
            return Math.Round(h, 4);
        }

        /// <summary>
        /// Calculates bias-corrected Shannon index using Chao & Shen (2003) method
        /// Accounts for unseen species in the sample
        /// </summary>
        public static double CalculateShannonBiasCorrected(double[] abundances)
        {
            double N = abundances.Sum();
            if (N == 0) return 0;

            // Count singletons (f1) and doubletons (f2)
            int f1 = abundances.Count(x => x == 1);
            int f2 = abundances.Count(x => x == 2);

            // Calculate sample coverage (Good-Turing estimator)
            double C = 1 - (f1 / N);
            if (C == 0) C = 0.001; // Avoid division by zero

            // Bias-corrected Shannon entropy
            double Hbc = 0;
            foreach (double n in abundances)
            {
                if (n > 0)
                {
                    double p = n / N;
                    // Coverage-adjusted frequency
                    double pAdj = C * p;
                    if (pAdj > 0 && pAdj < 1)
                    {
                        // Horvitz-Thompson adjustment
                        double term = pAdj * Math.Log(pAdj) / (1 - Math.Pow(1 - pAdj, N));
                        Hbc -= term;
                    }
                }
            }

            return Math.Round(Hbc, 4);
        }

        /// <summary>
        /// Calculates exp(H'bc) - Effective Number of Species
        /// Reference: Bouchet et al. (2012), Alve et al. (2009)
        /// </summary>
        public static double CalculateExpHbc(double[] abundances)
        {
            double hbc = CalculateShannonBiasCorrected(abundances);
            return Math.Round(Math.Exp(hbc), 4);
        }

        #endregion

        #region Sensitivity-Based Indices

        /// <summary>
        /// Calculates the Foram Stress Index (FSI)
        /// FSI = (10 Ã— Sen + Str) / (Sen + Str)
        /// Where Sen = proportion of stress-sensitive species, Str = proportion of stress-tolerant species
        /// Reference: Dimiza et al. (2016)
        /// Values: 0 (azoic) to 10 (optimal conditions)
        /// </summary>
        public static double CalculateFSI(double sensitivePercent, double tolerantPercent)
        {
            double sum = sensitivePercent + tolerantPercent;
            if (sum == 0) return 0; // Azoic

            double fsi = (10 * sensitivePercent + tolerantPercent) / sum;
            return Math.Round(fsi, 3);
        }

        /// <summary>
        /// Calculates the Ecological Quality Ratio (EQR) for positive indices
        /// EQR = Observed / Reference (capped at 1.0)
        /// Reference: EU Water Framework Directive
        /// </summary>
        /// <param name="observedValue">The observed index value</param>
        /// <param name="referenceValue">The reference (maximum expected) value</param>
        /// <returns>EQR value between 0 and 1</returns>
        public static double CalculateEQR(double observedValue, double referenceValue)
        {
            if (referenceValue <= 0) return 0;
            double eqr = observedValue / referenceValue;
            return Math.Round(Math.Min(1.0, Math.Max(0.0, eqr)), 4);
        }

        /// <summary>
        /// Gets the EQS classification from EQR value
        /// Based on EU Water Framework Directive boundaries
        /// </summary>
        public static string GetEQR_EQS(double eqr)
        {
            if (eqr >= 0.8) return "High";
            if (eqr >= 0.6) return "Good";
            if (eqr >= 0.4) return "Moderate";
            if (eqr >= 0.2) return "Poor";
            return "Bad";
        }

        /// <summary>
        /// Calculates the Tolerant Species Index for Mediterranean (TSI-Med)
        /// Uses the normalized formulation:
        /// TSI-Med = ((%TS - %TSref) / (100 - %TSref)) x 100
        /// where %TS is observed tolerant taxa percentage and %TSref is the
        /// expected reference percentage from sediment grain-size.
        /// Reference: Parent et al. (2021a,b), O'Brien et al. (2021)
        /// </summary>
        /// <param name="tolerantPercent">Percentage of tolerant species</param>
        /// <param name="mudPercent">Percentage of mud (grains < 63µm)</param>
        /// <param name="referenceType">Type of reference curve to use</param>
        /// <returns>TSI-Med value</returns>
        public static double CalculateTSIMed(double tolerantPercent, double mudPercent = 50,
            TSIReferenceType referenceType = TSIReferenceType.Barras2014_150um)
        {
            double tsRef = CalculateTSIReference(mudPercent, referenceType);
            double denominator = 100.0 - tsRef;
            if (Math.Abs(denominator) < 0.001) return 0;

            double tsiMed = ((tolerantPercent - tsRef) / denominator) * 100.0;
            return Math.Round(tsiMed, 3);
        }

        /// <summary>
        /// Calculates the TSI reference value based on mud percentage and reference curve type
        /// </summary>
        /// <param name="mudPercent">Percentage of mud (grains < 63µm)</param>
        /// <param name="referenceType">Type of reference curve to use</param>
        /// <returns>Expected percentage of tolerant species under natural conditions</returns>
        public static double CalculateTSIReference(double mudPercent, TSIReferenceType referenceType)
        {
            return referenceType switch
            {
                // Barras et al. (2014) - original curve for >150 µm fraction
                // %TSref = 5.0 + 0.3 × %mud
                TSIReferenceType.Barras2014_150um => 5.0 + 0.3 * mudPercent,

                // Parent et al. (2021b) - curve for >125 µm fraction (FOBIMO)
                // Different coefficients for >125 µm sieve size
                TSIReferenceType.Parent2021_125um => 4.5 + 0.28 * mudPercent,

                // Parent et al. (2021a) - using Jorissen et al. (2018) tolerant species list
                // %TSref = 0.3247 × %mud + 3.6718
                TSIReferenceType.Jorissen2018_125um_Homogenized => 3.6718 + 0.3247 * mudPercent,

                _ => 5.0 + 0.3 * mudPercent
            };
        }

        /// <summary>
        /// Calculates the Norwegian Quality Index for foraminifera (NQIf)
        /// NQIf = 0.5 x (1 - F-AMBI/7) + 0.5 x (ES100/35)
        /// where ES100 is Hurlbert rarefaction at n=100.
        /// Reference: Alve et al. (2019), O'Brien et al. (2021)
        /// </summary>
        public static double CalculateNQIf(double ambi, double es100)
        {
            if (double.IsNaN(ambi) || double.IsNaN(es100)) return double.NaN;

            double ambiComponent = Math.Max(0, Math.Min(1, 1 - (ambi / 7.0)));
            double esComponent = Math.Max(0, Math.Min(1, es100 / 35.0));
            double nqif = 0.5 * ambiComponent + 0.5 * esComponent;
            return Math.Round(nqif, 4);
        }

        /// <summary>
        /// Calculates the Foraminiferal Index of Environmental Impact (FIEI)
        /// FIEI = ((nr + no) / NTOT) Ã— 100
        /// Where nr = pollution-resistant taxa count, no = opportunistic taxa count
        /// Reference: Mojtahid et al. (2006), Denoyelle et al. (2010)
        /// </summary>
        public static double CalculateFIEI(double resistantCount, double opportunisticCount, double totalCount)
        {
            if (totalCount == 0) return 0;
            double fiei = ((resistantCount + opportunisticCount) / totalCount) * 100;
            return Math.Round(fiei, 3);
        }

        /// <summary>
        /// Calculates the FoRAM Index
        /// FI = (10 Ã— Ps) + Po + (2 Ã— Ph)
        /// Where Ps = symbiont-bearing proportion, Po = stress-tolerant proportion, Ph = other heterotrophic proportion
        /// Reference: Hallock et al. (2003), Prazeres et al. (2020)
        /// Values > 4: suitable for coral growth, 2-4: marginal, < 2: unsuitable
        /// </summary>
        public static double CalculateFoRAMIndex(double symbiontPercent, double stressTolerantPercent, double heterotrophicPercent)
        {
            double ps = symbiontPercent / 100;
            double po = stressTolerantPercent / 100;
            double ph = heterotrophicPercent / 100;

            double fi = (10 * ps) + po + (2 * ph);
            return Math.Round(fi, 3);
        }

        /// <summary>
        /// Calculates the BENTIX biotic index
        /// BENTIX = (6 x %GS + 2 x %GT) / 100
        /// Where:
        ///   GS = Sensitive group (EG1 + EG2: sensitive + indifferent species)
        ///   GT = Tolerant group (EG3 + EG4 + EG5: tolerant + opportunistic species)
        /// Reference: Simboura N. & Zenetos A. (2002) Mediterranean Marine Science, 3/2: 77-111
        /// Values range from 2 (all tolerant) to 6 (all sensitive)
        /// </summary>
        /// <param name="ecoGroupPercentages">Array of 5 ecological group percentages [EG1%, EG2%, EG3%, EG4%, EG5%]</param>
        /// <returns>BENTIX value (2-6 scale)</returns>
        public static double CalculateBENTIX(double[] ecoGroupPercentages)
        {
            if (ecoGroupPercentages == null || ecoGroupPercentages.Length != 5)
                return double.NaN;

            // GS = Sensitive group: EG1 (sensitive) + EG2 (indifferent)
            double gs = ecoGroupPercentages[0] + ecoGroupPercentages[1];

            // GT = Tolerant group: EG3 (tolerant) + EG4 (first-order opportunistic) + EG5 (second-order opportunistic)
            double gt = ecoGroupPercentages[2] + ecoGroupPercentages[3] + ecoGroupPercentages[4];

            // Verify percentages sum to ~100%
            double total = gs + gt;
            if (total == 0) return 0; // Azoic

            // BENTIX formula: (6 x %GS + 2 x %GT) / 100
            // The weights are based on the probability ratio of 3:1 (tolerant:sensitive) multiplied by 2
            double bentix = (6 * gs + 2 * gt) / 100;

            return Math.Round(bentix, 3);
        }

        /// <summary>
        /// Alternative BENTIX calculation using pre-calculated sensitive and tolerant percentages
        /// </summary>
        /// <param name="sensitivePercent">Percentage of sensitive group (GS = EG1 + EG2)</param>
        /// <param name="tolerantPercent">Percentage of tolerant group (GT = EG3 + EG4 + EG5)</param>
        /// <returns>BENTIX value (2-6 scale)</returns>
        public static double CalculateBENTIX(double sensitivePercent, double tolerantPercent)
        {
            double total = sensitivePercent + tolerantPercent;
            if (total == 0) return 0; // Azoic

            double bentix = (6 * sensitivePercent + 2 * tolerantPercent) / 100;
            return Math.Round(bentix, 3);
        }

        /// <summary>
        /// Calculates the Benthic Quality Index (BQI) adapted for foraminifera
        /// BQI = log10(S+1) x mean_sensitivity
        /// Where mean_sensitivity = sum(ni/N x sensitivity_i) for all species
        /// Sensitivity values are derived from ecological groups:
        ///   EG1 (sensitive) = 15, EG2 (indifferent) = 12, EG3 (tolerant) = 8,
        ///   EG4 (1st opportunistic) = 4, EG5 (2nd opportunistic) = 1
        /// Reference: Rosenberg R. et al. (2004) Marine Pollution Bulletin 49:728-739
        /// Adapted for foraminifera using F-AMBI ecological groups as sensitivity proxies
        /// </summary>
        /// <param name="speciesRichness">Number of species (S)</param>
        /// <param name="ecoGroupPercentages">Array of 5 ecological group percentages [EG1%, EG2%, EG3%, EG4%, EG5%]</param>
        /// <param name="totalAbundance">Total number of individuals (N), used for low-density correction</param>
        /// <returns>BQI value (typically 0-20 range)</returns>
        public static double CalculateBQI(int speciesRichness, double[] ecoGroupPercentages, double totalAbundance = 0)
        {
            if (speciesRichness == 0 || ecoGroupPercentages == null || ecoGroupPercentages.Length != 5)
                return 0; // Azoic

            // Sensitivity values based on ecological groups (adapted from ES50 concept)
            // Higher values = more sensitive species, typically found in pristine environments
            double[] sensitivityValues = { 15, 12, 8, 4, 1 };

            // Calculate abundance-weighted mean sensitivity
            // Each eco-group percentage represents the proportion of that group
            double meanSensitivity = 0;
            double totalPercent = 0;

            for (int i = 0; i < 5; i++)
            {
                meanSensitivity += (ecoGroupPercentages[i] / 100.0) * sensitivityValues[i];
                totalPercent += ecoGroupPercentages[i];
            }

            if (totalPercent == 0) return 0; // Azoic

            // BQI formula (Rosenberg et al. 2004, Josefson et al. 2009):
            // BQI = mean_sensitivity x log10(S+1) x N/(N+5)
            // The N/(N+5) factor corrects for low-density samples
            double bqi = meanSensitivity * Math.Log10(speciesRichness + 1);

            // Apply low-density correction factor if abundance is provided
            if (totalAbundance > 0)
            {
                bqi *= totalAbundance / (totalAbundance + 5.0);
            }

            return Math.Round(bqi, 3);
        }

        #endregion

        #region Ecological Quality Status Classifications

        /// <summary>
        /// FSI Ecological Quality Status classes (Dimiza et al. 2016)
        /// </summary>
        public static string GetFSI_EQS(double fsi)
        {
            if (fsi == 0) return "Azoic";
            if (fsi >= 9) return "High";
            if (fsi >= 5.5) return "Good";
            if (fsi >= 2) return "Moderate";
            if (fsi >= 1) return "Poor";
            return "Bad";
        }

        /// <summary>
        /// FoRAM Index interpretation (Hallock et al. 2003)
        /// </summary>
        public static string GetFoRAM_Status(double fi)
        {
            if (fi > 4) return "Suitable for coral growth";
            if (fi >= 2) return "Marginal conditions";
            return "Unsuitable for coral growth";
        }

        /// <summary>
        /// FAMBI Ecological Quality Status classes
        /// Supports different threshold systems
        /// </summary>
        /// <param name="fambi">Foram-AMBI value</param>
        /// <param name="thresholdType">Threshold system to use</param>
        public static string GetFAMBI_EQS(double fambi, FAMBIThresholdType thresholdType = FAMBIThresholdType.Borja2003)
        {
            return thresholdType switch
            {
                // Borja et al. (2003) - Traditional thresholds
                // High ≤1.2, Good ≤3.3, Moderate ≤4.3, Poor ≤5.5, Bad >5.5
                FAMBIThresholdType.Borja2003 => fambi switch
                {
                    <= 1.2 => "High",
                    <= 3.3 => "Good",
                    <= 4.3 => "Moderate",
                    <= 5.5 => "Poor",
                    _ => "Bad"
                },

                // Parent et al. (2021b) - Updated Foram-AMBI thresholds
                // Very good: 0 ≤ x < 1.4; Good: 1.4 ≤ x < 2.4; Moderate: 2.4 ≤ x < 3.4;
                // Poor: 3.4 ≤ x < 4.4; Bad: 4.4 ≤ x ≤ 6
                FAMBIThresholdType.Parent2021 => fambi switch
                {
                    < 1.4 => "High",
                    < 2.4 => "Good",
                    < 3.4 => "Moderate",
                    < 4.4 => "Poor",
                    _ => "Bad"
                },

                // Bouchet et al. (2025) - Brazilian transitional waters
                // High <1.4, Good 1.4-1.8, Moderate 1.8-3.0, Poor 3.0-4.0, Bad >4.0
                FAMBIThresholdType.Bouchet2025Brazil => fambi switch
                {
                    < 1.4 => "High",
                    < 1.8 => "Good",
                    < 3.0 => "Moderate",
                    <= 4.0 => "Poor",
                    _ => "Bad"
                },

                // Default to Borja2003
                _ => fambi <= 1.2 ? "High" : fambi <= 3.3 ? "Good" : fambi <= 4.3 ? "Moderate" : fambi <= 5.5 ? "Poor" : "Bad"
            };
        }

        /// <summary>
        /// TSI-Med Ecological Quality Status classes
        /// Supports multiple threshold conventions.
        /// </summary>
        public static string GetTSIMed_EQS(double tsiMed,
            TSIThresholdType thresholdType = TSIThresholdType.Parent2021)
        {
            return thresholdType switch
            {
                // Parent et al. (2021): lower values indicate better status
                TSIThresholdType.Parent2021 => tsiMed switch
                {
                    <= 4 => "High",
                    <= 16 => "Good",
                    <= 36 => "Moderate",
                    <= 64 => "Poor",
                    _ => "Bad"
                },

                // Barras & Jorissen (2011) convention requested in chapter review comments
                // lower values indicate poorer status
                TSIThresholdType.BarrasJorissen2011 => tsiMed switch
                {
                    <= 4 => "Bad",
                    <= 16 => "Poor",
                    <= 36 => "Moderate",
                    <= 64 => "Good",
                    _ => "High"
                },

                _ => tsiMed <= 4 ? "High" : tsiMed <= 16 ? "Good" : tsiMed <= 36 ? "Moderate" : tsiMed <= 64 ? "Poor" : "Bad"
            };
        }

        /// <summary>
        /// NQI Ecological Quality Status classes (Norwegian system)
        /// </summary>
        public static string GetNQI_EQS(double nqi)
        {
            if (nqi >= 0.54) return "High";
            if (nqi >= 0.45) return "Good";
            if (nqi >= 0.31) return "Moderate";
            if (nqi >= 0.13) return "Poor";
            return "Bad";
        }

        /// <summary>
        /// exp(H'bc) Ecological Quality Status
        /// Threshold systems from O'Brien et al. (2021), Table 3.
        /// </summary>
        public static string GetExpHbc_EQS(double expHbc,
            ExpHbcThresholdType thresholdType = ExpHbcThresholdType.OBrien2021_Norwegian63um)
        {
            return thresholdType switch
            {
                // (A) Norwegian environments, >125 µm fraction
                ExpHbcThresholdType.OBrien2021_Norwegian125um => expHbc switch
                {
                    >= 10 => "High",
                    >= 7 => "Good",
                    >= 5 => "Moderate",
                    > 2 => "Poor",
                    _ => "Bad"
                },

                // (B) Norwegian environments, >63 µm fraction
                ExpHbcThresholdType.OBrien2021_Norwegian63um => expHbc switch
                {
                    >= 22 => "High",
                    >= 13 => "Good",
                    >= 7 => "Moderate",
                    > 3 => "Poor",
                    _ => "Bad"
                },

                // (C) Italian transitional waters, >63 µm fraction
                ExpHbcThresholdType.OBrien2021_Italian63um => expHbc switch
                {
                    >= 5 => "High",
                    >= 4 => "Good",
                    >= 3 => "Moderate",
                    > 2 => "Poor",
                    _ => "Bad"
                },

                _ => expHbc >= 22 ? "High" : expHbc >= 13 ? "Good" : expHbc >= 7 ? "Moderate" : expHbc > 3 ? "Poor" : "Bad"
            };
        }

        /// <summary>
        /// BENTIX Ecological Quality Status classification (Simboura & Zenetos 2002)
        /// Based on Mediterranean soft-bottom marine ecosystems
        /// </summary>
        public static string GetBENTIX_EQS(double bentix)
        {
            if (bentix == 0) return "Azoic";
            if (bentix >= 4.5) return "High";
            if (bentix >= 3.5) return "Good";
            if (bentix >= 2.5) return "Moderate";
            if (bentix >= 2.0) return "Poor";
            return "Bad";
        }

        /// <summary>
        /// BQI Ecological Quality Status classification (adapted from Rosenberg et al. 2004)
        /// Thresholds adapted for foraminifera-based BQI using eco-group sensitivities
        /// </summary>
        public static string GetBQI_EQS(double bqi)
        {
            if (bqi == 0) return "Azoic";
            if (bqi >= 12) return "High";
            if (bqi >= 8) return "Good";
            if (bqi >= 5) return "Moderate";
            if (bqi >= 2) return "Poor";
            return "Bad";
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculate species richness (S)
        /// </summary>
        public static int CalculateSpeciesRichness(double[] abundances)
        {
            return abundances.Count(x => x > 0);
        }

        /// <summary>
        /// Calculate total abundance (N)
        /// </summary>
        public static double CalculateTotalAbundance(double[] abundances)
        {
            return abundances.Sum();
        }

        /// <summary>
        /// Calculate Simpson's dominance index (D)
        /// D = Î£(ni(ni-1)) / N(N-1)
        /// </summary>
        public static double CalculateSimpsonDominance(double[] abundances)
        {
            double N = abundances.Sum();
            if (N <= 1) return 0;

            double sum = 0;
            foreach (double n in abundances)
            {
                if (n > 1)
                {
                    sum += n * (n - 1);
                }
            }
            return Math.Round(sum / (N * (N - 1)), 4);
        }

        /// <summary>
        /// Calculate Simpson's diversity index (1 - D)
        /// </summary>
        public static double CalculateSimpsonDiversity(double[] abundances)
        {
            return Math.Round(1 - CalculateSimpsonDominance(abundances), 4);
        }

        /// <summary>
        /// Calculate Pielou's evenness (J')
        /// J' = H' / ln(S)
        /// </summary>
        public static double CalculatePielousEvenness(double[] abundances)
        {
            double h = CalculateShannonLn(abundances);
            int s = CalculateSpeciesRichness(abundances);
            if (s <= 1) return 0;
            return Math.Round(h / Math.Log(s), 4);
        }

        /// <summary>
        /// Calculate Hurlbert's rarefaction index ES(n)
        /// Expected number of species in a sample of n individuals
        /// </summary>
        public static double CalculateES(double[] abundances, int n = 100)
        {
            double N = abundances.Sum();
            if (N < n) return CalculateSpeciesRichness(abundances);

            double es = 0;
            foreach (double ni in abundances)
            {
                if (ni > 0)
                {
                    // Calculate binomial coefficient ratio
                    double term = 1;
                    for (int k = 0; k < n; k++)
                    {
                        term *= (N - ni - k) / (N - k);
                    }
                    es += (1 - term);
                }
            }
            return Math.Round(es, 4);
        }

        #endregion

        #region Foram-M-AMBI (Multivariate AMBI for Foraminifera)

        /// <summary>
        /// Calculates the Foram-M-AMBI (Multivariate AMBI adapted for foraminifera)
        /// Based on Muxika et al. (2007) methodology
        /// Combines F-AMBI, Shannon diversity (H'), and Species richness (S) in multivariate space
        /// Reference: Muxika I., Borja A., Bald J. (2007) Marine Pollution Bulletin 55:16-29
        /// </summary>
        /// <param name="fambi">F-AMBI value (0-6 scale)</param>
        /// <param name="shannonH">Shannon diversity index (natural log)</param>
        /// <param name="speciesRichness">Number of species (S)</param>
        /// <param name="referenceConditions">Reference conditions for the habitat type</param>
        /// <returns>Foram-M-AMBI EQR value (0-1 scale)</returns>
        public static double CalculateForamMAMBI(double fambi, double shannonH, int speciesRichness, 
            ForamMAMBIReferenceConditions referenceConditions)
        {
            if (referenceConditions == null)
            {
                // Use default Mediterranean/Atlantic coastal reference conditions
                referenceConditions = ForamMAMBIReferenceConditions.GetDefaultCoastal();
            }

            // Handle azoic samples
            if (speciesRichness == 0 || double.IsNaN(fambi))
            {
                return 0.0;
            }

            // Step 1: Calculate normalized values for each metric
            // For AMBI: inverse relationship (lower AMBI = better quality)
            // Normalized_AMBI = (Bad_AMBI - Observed_AMBI) / (Bad_AMBI - High_AMBI)
            double normAMBI = 0;
            if (Math.Abs(referenceConditions.Bad_FAMBI - referenceConditions.High_FAMBI) > 0.001)
            {
                normAMBI = (referenceConditions.Bad_FAMBI - fambi) / 
                           (referenceConditions.Bad_FAMBI - referenceConditions.High_FAMBI);
            }
            normAMBI = Math.Max(0, Math.Min(1, normAMBI));

            // For H': direct relationship (higher H' = better quality)
            // Normalized_H = (Observed_H - Bad_H) / (High_H - Bad_H)
            double normH = 0;
            if (Math.Abs(referenceConditions.High_Shannon - referenceConditions.Bad_Shannon) > 0.001)
            {
                normH = (shannonH - referenceConditions.Bad_Shannon) / 
                        (referenceConditions.High_Shannon - referenceConditions.Bad_Shannon);
            }
            normH = Math.Max(0, Math.Min(1, normH));

            // For S: direct relationship (higher S = better quality)
            // Normalized_S = (Observed_S - Bad_S) / (High_S - Bad_S)
            double normS = 0;
            if (Math.Abs(referenceConditions.High_Richness - referenceConditions.Bad_Richness) > 0.001)
            {
                normS = (speciesRichness - referenceConditions.Bad_Richness) / 
                        (referenceConditions.High_Richness - referenceConditions.Bad_Richness);
            }
            normS = Math.Max(0, Math.Min(1, normS));

            // Step 2: Apply Factor Analysis weights (from Muxika et al. 2007)
            // The three metrics are combined using empirically derived weights
            // Default weights based on Factor Analysis loadings from the original study
            double w_AMBI = referenceConditions.Weight_FAMBI;
            double w_H = referenceConditions.Weight_Shannon;
            double w_S = referenceConditions.Weight_Richness;

            // Normalize weights to sum to 1
            double totalWeight = w_AMBI + w_H + w_S;
            if (totalWeight > 0)
            {
                w_AMBI /= totalWeight;
                w_H /= totalWeight;
                w_S /= totalWeight;
            }
            else
            {
                // Equal weights as fallback
                w_AMBI = w_H = w_S = 1.0 / 3.0;
            }

            // Step 3: Calculate M-AMBI as weighted combination
            double mAMBI = (w_AMBI * normAMBI) + (w_H * normH) + (w_S * normS);

            // Ensure result is within valid range
            mAMBI = Math.Max(0, Math.Min(1, mAMBI));

            return Math.Round(mAMBI, 4);
        }

        /// <summary>
        /// Calculates Foram-M-AMBI using default reference conditions
        /// Suitable for general coastal/shelf environments
        /// </summary>
        public static double CalculateForamMAMBI(double fambi, double shannonH, int speciesRichness)
        {
            return CalculateForamMAMBI(fambi, shannonH, speciesRichness, null);
        }

        /// <summary>
        /// Calculates the Euclidean distance in multivariate space for M-AMBI
        /// Alternative calculation method using geometric distance from reference point
        /// </summary>
        public static double CalculateForamMAMBI_Euclidean(double fambi, double shannonH, int speciesRichness,
            ForamMAMBIReferenceConditions referenceConditions)
        {
            if (referenceConditions == null)
            {
                referenceConditions = ForamMAMBIReferenceConditions.GetDefaultCoastal();
            }

            if (speciesRichness == 0 || double.IsNaN(fambi))
            {
                return 0.0;
            }

            // Normalize each metric to 0-1 scale
            double normAMBI = (referenceConditions.Bad_FAMBI - fambi) / 
                              (referenceConditions.Bad_FAMBI - referenceConditions.High_FAMBI);
            double normH = (shannonH - referenceConditions.Bad_Shannon) / 
                           (referenceConditions.High_Shannon - referenceConditions.Bad_Shannon);
            double normS = (speciesRichness - referenceConditions.Bad_Richness) / 
                           (referenceConditions.High_Richness - referenceConditions.Bad_Richness);

            // Clamp to valid range
            normAMBI = Math.Max(0, Math.Min(1, normAMBI));
            normH = Math.Max(0, Math.Min(1, normH));
            normS = Math.Max(0, Math.Min(1, normS));

            // Reference point for "High" status is (1, 1, 1) in normalized space
            // Reference point for "Bad" status is (0, 0, 0) in normalized space
            // Calculate distance from Bad point
            double distFromBad = Math.Sqrt(normAMBI * normAMBI + normH * normH + normS * normS);
            
            // Maximum possible distance is sqrt(3) ≈ 1.732
            double maxDist = Math.Sqrt(3);

            // EQR = distance from Bad / maximum distance
            double eqr = distFromBad / maxDist;

            return Math.Round(eqr, 4);
        }

        /// <summary>
        /// Gets the Ecological Quality Status classification for Foram-M-AMBI
        /// Based on WFD boundary values from Muxika et al. (2007)
        /// </summary>
        public static string GetForamMAMBI_EQS(double mAMBI)
        {
            if (double.IsNaN(mAMBI)) return "N/A";
            if (mAMBI >= 0.81) return "High";
            if (mAMBI >= 0.61) return "Good";
            if (mAMBI >= 0.41) return "Moderate";
            if (mAMBI >= 0.21) return "Poor";
            return "Bad";
        }

        /// <summary>
        /// Gets detailed interpretation of Foram-M-AMBI result
        /// </summary>
        public static string GetForamMAMBI_Interpretation(double mAMBI, double fambi, double shannonH, int speciesRichness)
        {
            string eqs = GetForamMAMBI_EQS(mAMBI);
            
            // Identify which component is driving the result
            var refCond = ForamMAMBIReferenceConditions.GetDefaultCoastal();
            
            double normAMBI = (refCond.Bad_FAMBI - fambi) / (refCond.Bad_FAMBI - refCond.High_FAMBI);
            double normH = (shannonH - refCond.Bad_Shannon) / (refCond.High_Shannon - refCond.Bad_Shannon);
            double normS = (speciesRichness - refCond.Bad_Richness) / (refCond.High_Richness - refCond.Bad_Richness);

            normAMBI = Math.Max(0, Math.Min(1, normAMBI));
            normH = Math.Max(0, Math.Min(1, normH));
            normS = Math.Max(0, Math.Min(1, normS));

            string limitingFactor = "";
            double minNorm = Math.Min(normAMBI, Math.Min(normH, normS));
            
            if (minNorm == normAMBI && normAMBI < 0.5)
                limitingFactor = "F-AMBI indicates stress-tolerant assemblage";
            else if (minNorm == normH && normH < 0.5)
                limitingFactor = "Low diversity (H') indicates degradation";
            else if (minNorm == normS && normS < 0.5)
                limitingFactor = "Low species richness indicates impoverished fauna";

            return string.IsNullOrEmpty(limitingFactor) ? eqs : $"{eqs} - {limitingFactor}";
        }

        #endregion

        #region Eco-Group Calculations

        /// <summary>
        /// Calculate ecological group percentages from abundances and assignments
        /// Returns array [EG1%, EG2%, EG3%, EG4%, EG5%]
        /// </summary>
        public static double[] CalculateEcoGroupPercentages(Dictionary<string, double> speciesAbundances,
            Dictionary<string, int> ecoGroupAssignments)
        {
            double[] ecoGroups = new double[5];
            double total = 0;
            double assigned = 0;

            foreach (var species in speciesAbundances)
            {
                string speciesName = species.Key.Trim();
                double abundance = species.Value;
                total += abundance;

                // Try to find eco-group assignment using multiple matching strategies
                int eg = TryGetEcoGroup(speciesName, ecoGroupAssignments);

                if (eg >= 1 && eg <= 5)
                {
                    ecoGroups[eg - 1] += abundance;
                    assigned += abundance;
                }
            }

            if (total > 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    ecoGroups[i] = (ecoGroups[i] / total) * 100;
                }
            }

            return ecoGroups;
        }

        /// <summary>
        /// Tries to find eco-group assignment for a species using multiple matching strategies:
        /// 1. Direct/exact match (case-insensitive)
        /// 2. Normalized name match (removes author citations like "(d'Orbigny, 1839)")
        /// 3. Genus-level fallback (e.g., "Ammonia sp." if "Ammonia beccarii" not found)
        /// </summary>
        private static int TryGetEcoGroup(string speciesName, Dictionary<string, int> ecoGroupAssignments)
        {
            // Strategy 1: Direct match (dictionary is case-insensitive)
            if (ecoGroupAssignments.TryGetValue(speciesName, out int eg))
            {
                return eg;
            }

            // Strategy 2: Normalized name (remove author citations)
            string normalizedName = SpeciesNameMatcher.NormalizeSpeciesName(speciesName);
            if (!string.IsNullOrEmpty(normalizedName) && normalizedName != speciesName)
            {
                if (ecoGroupAssignments.TryGetValue(normalizedName, out eg))
                {
                    return eg;
                }
            }

            // Strategy 3: Genus-level fallback (first word + "sp.")
            string[] parts = normalizedName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1)
            {
                string genusMatch = parts[0] + " sp.";
                if (ecoGroupAssignments.TryGetValue(genusMatch, out eg))
                {
                    return eg;
                }
            }

            // No match found
            return 0;
        }

        /// <summary>
        /// Calculate FAMBI from eco-group percentages
        /// FAMBI = (0Ã—EG1 + 1.5Ã—EG2 + 3Ã—EG3 + 4.5Ã—EG4 + 6Ã—EG5) / 100
        /// </summary>
        public static double CalculateFAMBI(double[] ecoGroupPercentages)
        {
            if (ecoGroupPercentages.Length != 5) return 0;

            double fambi = (0 * ecoGroupPercentages[0] +
                           1.5 * ecoGroupPercentages[1] +
                           3 * ecoGroupPercentages[2] +
                           4.5 * ecoGroupPercentages[3] +
                           6 * ecoGroupPercentages[4]) / 100;

            return Math.Round(fambi, 3);
        }

        #endregion
    }

    /// <summary>
    /// Result container for all calculated indices for a single sample
    /// </summary>
    public class IndicesResult
    {
        public string SampleName { get; set; }

        // Diversity indices
        public double Shannon_Ln { get; set; }
        public double Shannon_Log2 { get; set; }
        public double Shannon_BC { get; set; }
        public double Exp_Hbc { get; set; }
        public double Simpson_D { get; set; }
        public double Simpson_1D { get; set; }
        public double Pielou_J { get; set; }
        public double ES100 { get; set; }
        public int SpeciesRichness { get; set; }
        public double TotalAbundance { get; set; }

        // Sensitivity indices
        public double FAMBI { get; set; }
        public double FSI { get; set; }
        public double TSI_Med { get; set; }
        public double NQIf { get; set; }
        public double FIEI { get; set; }
        public double FoRAM_Index { get; set; }
        public double BENTIX { get; set; }
        public double BQI { get; set; }

        // Eco-group percentages
        public double[] EcoGroups { get; set; } = new double[5];

        // NEW: Databank tracking properties
        public double FSI_AssignedPercent { get; set; }
        public bool FSI_UsingSpecializedDatabank { get; set; }
        public double TSIMed_TolerantPercent { get; set; }
        public bool TSIMed_UsingSpecializedDatabank { get; set; }
        public double MudPercent { get; set; } = 50.0;
        public bool FoRAM_ApplicableEnvironment { get; set; } = false;

        // TSI-Med configuration
        public TSIReferenceType TSIMed_ReferenceType { get; set; } = TSIReferenceType.Barras2014_150um;
        public double TSIMed_ReferenceValue { get; set; }
        public bool TSIMed_UsingJorissenList { get; set; } = false;
        public TSIThresholdType TSIMed_ThresholdType { get; set; } = TSIThresholdType.Parent2021;

        // EQR (Ecological Quality Ratio) for positive indices
        public double FSI_EQR { get; set; }
        public double FSI_ReferenceValue { get; set; } = 10.0; // Maximum FSI value
        public double ExpHbc_EQR { get; set; }
        public double ExpHbc_ReferenceValue { get; set; } = 20.0; // Expected reference value

        // FAMBI threshold type
        public FAMBIThresholdType FAMBI_ThresholdType { get; set; } = FAMBIThresholdType.Borja2003;
        public ExpHbcThresholdType ExpHbc_ThresholdType { get; set; } = ExpHbcThresholdType.OBrien2021_Norwegian63um;

        // FoRAM Index functional group percentages
        public double FoRAM_SymbiontPercent { get; set; }
        public double FoRAM_StressTolerantPercent { get; set; }
        public double FoRAM_HeterotrophicPercent { get; set; }
        public double FoRAM_AssignedPercent { get; set; }

        // EQS classifications
        public string FAMBI_EQS => BioticIndicesCalculator.GetFAMBI_EQS(FAMBI, FAMBI_ThresholdType);
        public string FSI_EQS => double.IsNaN(FSI) ? "N/A" : BioticIndicesCalculator.GetFSI_EQS(FSI);
        public string FSI_EQR_EQS => double.IsNaN(FSI_EQR) ? "N/A" : BioticIndicesCalculator.GetEQR_EQS(FSI_EQR);
        public string NQI_EQS => BioticIndicesCalculator.GetNQI_EQS(NQIf);
        public string ExpHbc_EQS => BioticIndicesCalculator.GetExpHbc_EQS(Exp_Hbc, ExpHbc_ThresholdType);
        public string ExpHbc_EQR_EQS => double.IsNaN(ExpHbc_EQR) ? "N/A" : BioticIndicesCalculator.GetEQR_EQS(ExpHbc_EQR);
        public string TSIMed_EQS => double.IsNaN(TSI_Med) ? "N/A" : BioticIndicesCalculator.GetTSIMed_EQS(TSI_Med, TSIMed_ThresholdType);
        public string BENTIX_EQS => double.IsNaN(BENTIX) ? "N/A" : BioticIndicesCalculator.GetBENTIX_EQS(BENTIX);
        public string BQI_EQS => double.IsNaN(BQI) ? "N/A" : BioticIndicesCalculator.GetBQI_EQS(BQI);
        public string FoRAM_Status
        {
            get
            {
                if (double.IsNaN(FoRAM_Index))
                    return "Not calculated (non-tropical environment)";
                if (!FoRAM_ApplicableEnvironment)
                    return "Warning: Index only valid for tropical coral reefs";
                return BioticIndicesCalculator.GetFoRAM_Status(FoRAM_Index);
            }
        }

        // Foram-M-AMBI (Multivariate AMBI for Foraminifera)
        public double ForamMAMBI { get; set; }
        public double ForamMAMBI_Euclidean { get; set; }
        public double ForamMAMBI_NormAMBI { get; set; }
        public double ForamMAMBI_NormH { get; set; }
        public double ForamMAMBI_NormS { get; set; }
        public string ForamMAMBI_EQS => BioticIndicesCalculator.GetForamMAMBI_EQS(ForamMAMBI);
        public string ForamMAMBI_Interpretation =>
            BioticIndicesCalculator.GetForamMAMBI_Interpretation(ForamMAMBI, FAMBI, Shannon_Ln, SpeciesRichness);
    }

    /// <summary>
    /// Reference conditions for Foram-M-AMBI calculation
    /// Based on Muxika et al. (2007) methodology adapted for foraminifera
    /// Contains reference values for "High" (pristine) and "Bad" (degraded) status
    /// plus Factor Analysis weights for combining the three metrics
    /// </summary>
    public class ForamMAMBIReferenceConditions
    {
        /// <summary>
        /// Name/identifier for this reference condition set
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the habitat type these conditions apply to
        /// </summary>
        public string Description { get; set; }

        // Reference values for "High" (pristine/undisturbed) status
        public double High_FAMBI { get; set; }
        public double High_Shannon { get; set; }
        public double High_Richness { get; set; }

        // Reference values for "Bad" (azoic/severely degraded) status
        public double Bad_FAMBI { get; set; }
        public double Bad_Shannon { get; set; }
        public double Bad_Richness { get; set; }

        // Factor Analysis weights for combining metrics
        // These should sum to approximately 1.0 (will be normalized)
        public double Weight_FAMBI { get; set; }
        public double Weight_Shannon { get; set; }
        public double Weight_Richness { get; set; }

        /// <summary>
        /// Default reference conditions for coastal/shelf environments
        /// Based on literature values for Mediterranean and Atlantic foraminifera
        /// </summary>
        public static ForamMAMBIReferenceConditions GetDefaultCoastal()
        {
            return new ForamMAMBIReferenceConditions
            {
                Name = "Coastal/Shelf Default",
                Description = "Default reference conditions for coastal and continental shelf environments",
                
                // High status: pristine conditions
                // F-AMBI typically 0-1.2 for high status (from Borja et al. 2000)
                High_FAMBI = 0.5,
                // Shannon diversity H' typically 2.5-4.0 in pristine foram assemblages
                High_Shannon = 3.5,
                // Species richness typically 20-50 species in healthy assemblages
                High_Richness = 35,

                // Bad status: severely degraded/azoic conditions
                // F-AMBI > 5.5 indicates bad status
                Bad_FAMBI = 6.0,
                // Very low diversity in degraded environments
                Bad_Shannon = 0.0,
                // Azoic or near-azoic
                Bad_Richness = 0,

                // Equal weights as default (Factor Analysis would derive these empirically)
                // Based on Muxika et al. (2007), AMBI tends to have slightly higher weight
                Weight_FAMBI = 0.40,
                Weight_Shannon = 0.35,
                Weight_Richness = 0.25
            };
        }

        /// <summary>
        /// Reference conditions for Mediterranean transitional waters (lagoons, estuaries)
        /// Higher natural stress tolerance expected
        /// </summary>
        public static ForamMAMBIReferenceConditions GetMediterraneanTransitional()
        {
            return new ForamMAMBIReferenceConditions
            {
                Name = "Mediterranean Transitional",
                Description = "Reference conditions for Mediterranean lagoons and transitional waters",
                
                High_FAMBI = 1.0,  // Slightly higher natural stress
                High_Shannon = 2.8,
                High_Richness = 25,

                Bad_FAMBI = 6.0,
                Bad_Shannon = 0.0,
                Bad_Richness = 0,

                Weight_FAMBI = 0.45,
                Weight_Shannon = 0.30,
                Weight_Richness = 0.25
            };
        }

        /// <summary>
        /// Reference conditions for deep-sea/bathyal environments
        /// Lower diversity and abundance expected naturally
        /// </summary>
        public static ForamMAMBIReferenceConditions GetDeepSea()
        {
            return new ForamMAMBIReferenceConditions
            {
                Name = "Deep-Sea/Bathyal",
                Description = "Reference conditions for deep-sea and bathyal environments",
                
                High_FAMBI = 0.3,
                High_Shannon = 3.0,
                High_Richness = 30,

                Bad_FAMBI = 5.5,
                Bad_Shannon = 0.0,
                Bad_Richness = 0,

                Weight_FAMBI = 0.35,
                Weight_Shannon = 0.40,
                Weight_Richness = 0.25
            };
        }

        /// <summary>
        /// Reference conditions for tropical/subtropical environments
        /// Higher diversity expected
        /// </summary>
        public static ForamMAMBIReferenceConditions GetTropical()
        {
            return new ForamMAMBIReferenceConditions
            {
                Name = "Tropical/Subtropical",
                Description = "Reference conditions for tropical and subtropical coastal environments",
                
                High_FAMBI = 0.4,
                High_Shannon = 4.0,
                High_Richness = 50,

                Bad_FAMBI = 6.0,
                Bad_Shannon = 0.0,
                Bad_Richness = 0,

                Weight_FAMBI = 0.35,
                Weight_Shannon = 0.35,
                Weight_Richness = 0.30
            };
        }

        /// <summary>
        /// Creates custom reference conditions based on user input
        /// </summary>
        public static ForamMAMBIReferenceConditions CreateCustom(
            string name,
            double highFAMBI, double highShannon, double highRichness,
            double badFAMBI, double badShannon, double badRichness,
            double weightFAMBI = 0.33, double weightShannon = 0.33, double weightRichness = 0.33)
        {
            return new ForamMAMBIReferenceConditions
            {
                Name = name,
                Description = "User-defined custom reference conditions",
                High_FAMBI = highFAMBI,
                High_Shannon = highShannon,
                High_Richness = highRichness,
                Bad_FAMBI = badFAMBI,
                Bad_Shannon = badShannon,
                Bad_Richness = badRichness,
                Weight_FAMBI = weightFAMBI,
                Weight_Shannon = weightShannon,
                Weight_Richness = weightRichness
            };
        }

        /// <summary>
        /// Validates the reference conditions
        /// </summary>
        public bool IsValid()
        {
            // High status should have lower AMBI than Bad status
            if (High_FAMBI >= Bad_FAMBI) return false;
            
            // High status should have higher diversity than Bad status
            if (High_Shannon <= Bad_Shannon) return false;
            
            // High status should have higher richness than Bad status
            if (High_Richness <= Bad_Richness) return false;
            
            // Weights should be positive
            if (Weight_FAMBI <= 0 || Weight_Shannon <= 0 || Weight_Richness <= 0) return false;
            
            return true;
        }

        public override string ToString()
        {
            return $"{Name}: High(FAMBI={High_FAMBI}, H'={High_Shannon}, S={High_Richness}), " +
                   $"Bad(FAMBI={Bad_FAMBI}, H'={Bad_Shannon}, S={Bad_Richness})";
        }
    }

    /// <summary>
    /// Threshold systems for Foram-AMBI EQS classification
    /// </summary>
    public enum FAMBIThresholdType
    {
        /// <summary>
        /// Traditional thresholds from Borja et al. (2003)
        /// High ≤1.2, Good ≤3.3, Moderate ≤4.3, Poor ≤5.5, Bad >5.5
        /// Reference: doi:10.1016/S0025-326X(03)00090-0
        /// </summary>
        Borja2003,

        /// <summary>
        /// Updated Foram-AMBI thresholds from Parent et al. (2021b)
        /// High <1.4, Good <2.4, Moderate <3.4, Poor <4.4, Bad ≥4.4
        /// Reference: doi:10.3390/w13223193
        /// </summary>
        Parent2021,

        /// <summary>
        /// Brazilian transitional waters thresholds (Bouchet et al. 2025)
        /// High <1.4, Good <1.8, Moderate <3.0, Poor ≤4.0, Bad >4.0
        /// Reference: doi:10.5194/jm-44-237-2025
        /// </summary>
        Bouchet2025Brazil
    }

    /// <summary>
    /// TSI-Med threshold conventions.
    /// </summary>
    public enum TSIThresholdType
    {
        /// <summary>
        /// Parent et al. (2021) convention:
        /// ≤4 High, ≤16 Good, ≤36 Moderate, ≤64 Poor, >64 Bad.
        /// </summary>
        Parent2021,

        /// <summary>
        /// Barras & Jorissen (2011) convention used in chapter comments:
        /// ≤4 Bad, ≤16 Poor, ≤36 Moderate, ≤64 Good, >64 High.
        /// </summary>
        BarrasJorissen2011
    }

    /// <summary>
    /// exp(H'bc) threshold systems from O'Brien et al. (2021), Table 3.
    /// </summary>
    public enum ExpHbcThresholdType
    {
        /// <summary>
        /// Norwegian environments, >125 µm fraction.
        /// ≤2 Bad, 2-5 Poor, 5-7 Moderate, 7-10 Good, ≥10 High.
        /// </summary>
        OBrien2021_Norwegian125um,

        /// <summary>
        /// Norwegian environments, >63 µm fraction.
        /// ≤3 Bad, 3-7 Poor, 7-13 Moderate, 13-22 Good, ≥22 High.
        /// </summary>
        OBrien2021_Norwegian63um,

        /// <summary>
        /// Italian transitional waters, >63 µm fraction.
        /// ≤2 Bad, 2-3 Poor, 3-4 Moderate, 4-5 Good, ≥5 High.
        /// </summary>
        OBrien2021_Italian63um
    }

    /// <summary>
    /// Reference curve types for TSI-Med calculation
    /// Based on sieve size fraction and tolerant species list used
    /// </summary>
    public enum TSIReferenceType
    {
        /// <summary>
        /// Original curve from Barras et al. (2014) for >150 µm fraction
        /// %TSref = 5.0 + 0.3 × %mud
        /// Uses original tolerant species list
        /// Reference: doi:10.1016/j.ecolind.2013.09.028
        /// </summary>
        Barras2014_150um,

        /// <summary>
        /// Curve for >125 µm fraction (FOBIMO standard)
        /// %TSref = 4.5 + 0.28 × %mud
        /// Uses original tolerant species list
        /// Reference: Parent et al. (2021b) - doi:10.3390/w13223193
        /// </summary>
        Parent2021_125um,

        /// <summary>
        /// Curve using Jorissen et al. (2018) homogenized species list
        /// >125 µm fraction with EG3+EG4+EG5 as tolerant species
        /// %TSref = 0.3247 × %mud + 3.6718
        /// Reference: Bouchet et al. (2021) - doi:10.1016/j.marpolbul.2021.112071
        /// </summary>
        Jorissen2018_125um_Homogenized
    }

    /// <summary>
    /// Configuration options for TSI-Med calculation
    /// </summary>
    public class TSIMedConfiguration
    {
        /// <summary>
        /// Reference curve type to use
        /// </summary>
        public TSIReferenceType ReferenceType { get; set; } = TSIReferenceType.Barras2014_150um;

        /// <summary>
        /// Whether to use homogenized Jorissen et al. (2018) species list
        /// If true, tolerant species = EG3 + EG4 + EG5 from Foram-AMBI databank
        /// </summary>
        public bool UseJorissenSpeciesList { get; set; } = false;

        /// <summary>
        /// Sieve size fraction used for sample analysis
        /// </summary>
        public double SieveSizeMicrons { get; set; } = 150;

        /// <summary>
        /// Gets the recommended reference type based on sieve size and species list
        /// </summary>
        public static TSIReferenceType GetRecommendedReferenceType(double sieveSizeMicrons, bool useJorissenList)
        {
            if (useJorissenList)
                return TSIReferenceType.Jorissen2018_125um_Homogenized;

            return sieveSizeMicrons >= 150
                ? TSIReferenceType.Barras2014_150um
                : TSIReferenceType.Parent2021_125um;
        }

        public override string ToString()
        {
            return ReferenceType switch
            {
                TSIReferenceType.Barras2014_150um => "Barras et al. (2014) - >150 µm",
                TSIReferenceType.Parent2021_125um => "Parent et al. (2021b) - >125 µm",
                TSIReferenceType.Jorissen2018_125um_Homogenized => "Jorissen et al. (2018) - >125 µm Homogenized",
                _ => "Unknown"
            };
        }
    }
}
