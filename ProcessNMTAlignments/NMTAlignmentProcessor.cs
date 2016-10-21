using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessNMTAlignments
{
    public class NMTAlignmentProcessor
    {
        public NMTAlignmentProcessor()
        {
            nfi.CurrencyDecimalSeparator = ".";
            nfi.NumberDecimalSeparator = ".";
            nfi.PercentDecimalSeparator = ".";
            ReadMSDDictionary(null);
        }
        
        public void ReadMSDDictionary(string mosesFile)
        {
            msdDictionary = new HashSet<string>();
            msdDictionary.Add("UNK");

            if (mosesFile != null && File.Exists(mosesFile))
            {
                StreamReader sr = new StreamReader(mosesFile, Encoding.UTF8);

                char[] sep = { ' ' };
                char[] sep2 = { '|' };
                Console.WriteLine("Reading POS-tags:");
                int counter = 0;
                //Perform first pass through the data and count all tokens.
                while (!sr.EndOfStream)
                {
                    counter++;
                    if (counter % 2000 == 0)
                    {
                        Console.Write(".");
                        if (counter % 100000 == 0) Console.WriteLine(" - " + counter.ToString());
                    }
                    string line = sr.ReadLine().Trim();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string[] tokens = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string t in tokens)
                        {
                            string[] parts = t.Split(sep2, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3)
                            {
                                if (!msdDictionary.Contains(parts[parts.Length - 1])) msdDictionary.Add(parts[parts.Length - 1]);
                            }

                        }
                    }
                }
                sr.Close();
            }
        }

        public HashSet<string> msdDictionary = new HashSet<string>();

        public List<NMTAlignmentElement> GetMaxAlignments(string[] sourceSentenceTokens, string[] targetSentenceTokens, Matrix<double> alignmentMatrix, bool addGaps=false, bool addLowConfNonTranslatedWords = false)
        {
            int[] targetMaxAlignments = GetMaximum(alignmentMatrix, true); //Identifies the maximum source ID (column) for each target token (row)
            int[] sourceMaxAlignments = GetMaximum(alignmentMatrix, false); //Identifies the maximum target ID (column) for each source token (row)

            if (sourceSentenceTokens == null || targetSentenceTokens == null || sourceMaxAlignments == null || targetMaxAlignments == null || sourceSentenceTokens.Length != sourceMaxAlignments.Length || targetSentenceTokens.Length != targetMaxAlignments.Length)
            {
                throw new InvalidDataException("Either one of the parameters for alignment string pair acquisition is null or the source and target sentence list counts do not correspond to the source and target alignment counts!");
            }
            Dictionary<int, int> trgMaxForSourceIds = new Dictionary<int, int>();
            for (int i=0;i<sourceMaxAlignments.Length;i++)
            {
                trgMaxForSourceIds.Add(i,sourceMaxAlignments[i]);
            }
            Dictionary<int, bool> srcIdxThatAreMaxForTrg = new Dictionary<int, bool>();
            for (int i=0;i<targetMaxAlignments.Length;i++)
            {
                if (!srcIdxThatAreMaxForTrg.ContainsKey(targetMaxAlignments[i])) srcIdxThatAreMaxForTrg.Add(targetMaxAlignments[i],true);
            }

            List<NMTAlignmentElement> res = new List<NMTAlignmentElement>();
            HashSet<int> coveredSourceIdx = new HashSet<int>();
            

            //The process should be such that we:
            // 1) identify maximum 1 to 1 alignments;
            for (int i=0;i<targetMaxAlignments.Length;i++)
            {
                NMTAlignmentElement elem = new NMTAlignmentElement();
                elem.targetIdList.Add(i);
                elem.targetStrings.Add(targetSentenceTokens[i]);
                elem.targetString=targetSentenceTokens[i];
                int sourceIdx = targetMaxAlignments[i];
                if (sourceMaxAlignments[sourceIdx] == i && targetMaxAlignments[i] == sourceIdx)
                {
                    coveredSourceIdx.Add(sourceIdx);
                    elem.sourceIdList.Add(sourceIdx);
                    elem.sourceStrings.Add(sourceSentenceTokens[sourceIdx]);
                }
                res.Add(elem);
            }
            // 2) add source words to words that are translated;
            for (int i = 0; i < res.Count; i++)
            {
                if (res[i].sourceIdList.Count > 0)
                {
                    int currSourceIdx = res[i].sourceIdList[0];
                    //Perform leftward search for additional source words:
                    for (int idx = currSourceIdx - 1; idx >= 0; idx--)
                    {
                        if (sourceMaxAlignments[idx] == i && !srcIdxThatAreMaxForTrg.ContainsKey(idx) && !coveredSourceIdx.Contains(idx))
                        {
                            res[i].sourceIdList.Insert(0, idx);
                            res[i].sourceStrings.Insert(0, sourceSentenceTokens[idx]);
                            coveredSourceIdx.Add(idx);
                        }
                        else if (!addGaps)
                        {
                            break; //Without the break, there will not be any gaps!
                        }
                    }
                    //Perform rightward search for additional source words:
                    for (int idx = currSourceIdx + 1; idx < sourceSentenceTokens.Length; idx++)
                    {
                        if (sourceMaxAlignments[idx] == i && !srcIdxThatAreMaxForTrg.ContainsKey(idx) && !coveredSourceIdx.Contains(idx))
                        {
                            res[i].sourceIdList.Add(idx);
                            res[i].sourceStrings.Add(sourceSentenceTokens[idx]);
                            coveredSourceIdx.Add(idx);
                        }
                        else if (!addGaps)
                        {
                            break; //Without the break, there will not be any gaps!
                        }
                    }
                }
            }
            
            // 3) add non-linked target words to max source words if the source words are not linked to anything!
            for (int i = 0; i < res.Count; i++)
            {
                if (res[i].sourceIdList.Count < 1)
                {
                    if (!coveredSourceIdx.Contains(targetMaxAlignments[i]))
                    {
                        coveredSourceIdx.Add(targetMaxAlignments[i]);
                        res[i].sourceIdList.Add(targetMaxAlignments[i]);
                        res[i].sourceStrings.Add(sourceSentenceTokens[targetMaxAlignments[i]]);
                    }
                }
            }

            //TEST THIS!!!
            // 4) add remaining non-linked target words to max source words!
            for (int i = 0; i < res.Count; i++)
            {
                if (res[i].sourceIdList.Count < 1)
                {
                    if (!coveredSourceIdx.Contains(targetMaxAlignments[i]))
                    {
                        coveredSourceIdx.Add(targetMaxAlignments[i]);
                    }
                    res[i].sourceIdList.Add(targetMaxAlignments[i]);
                    res[i].sourceStrings.Add(sourceSentenceTokens[targetMaxAlignments[i]]);
                }
            }

            // 5) add non-linked source words to the max target words if the target words are not linked to anything!
            for (int i = 0; i < sourceSentenceTokens.Length; i++)
            {
                if (!coveredSourceIdx.Contains(i))
                {
                    int targetIdx = sourceMaxAlignments[i];
                    if (res[targetIdx].sourceIdList.Count < 1)
                    {
                        coveredSourceIdx.Add(i);
                        res[targetIdx].sourceIdList.Add(i);
                        res[targetIdx].sourceStrings.Add(sourceSentenceTokens[i]);
                    }
                }
            }
            
            // 6) add source words to words that are translated and have just one source word linked;
            for (int i = 0; i < res.Count; i++)
            {
                if (res[i].sourceIdList.Count == 1)
                {
                    int currSourceIdx = res[i].sourceIdList[0];
                    //Perform leftward search for additional source words:
                    for (int idx = currSourceIdx - 1; idx >= 0; idx--)
                    {
                        if (sourceMaxAlignments[idx] == i && !srcIdxThatAreMaxForTrg.ContainsKey(idx) && !coveredSourceIdx.Contains(idx))
                        {
                            res[i].sourceIdList.Insert(0, idx);
                            res[i].sourceStrings.Insert(0, sourceSentenceTokens[idx]);

                            coveredSourceIdx.Add(idx);
                        }
                        else //if (!addGaps) //This is commented out, because the current alignments are already ambiguous. Adding more ambiguous alignments with gaps is dangerous. 
                        {
                            break; //Without the break, there will not be any gaps!
                        }
                    }
                    //Perform rightward search for additional source words:
                    for (int idx = currSourceIdx + 1; idx < sourceSentenceTokens.Length; idx++)
                    {
                        if (sourceMaxAlignments[idx] == i && !srcIdxThatAreMaxForTrg.ContainsKey(idx) && !coveredSourceIdx.Contains(idx))
                        {
                            res[i].sourceIdList.Add(idx);
                            res[i].sourceStrings.Add(sourceSentenceTokens[idx]);

                            coveredSourceIdx.Add(idx);
                        }
                        else //if (!addGaps)
                        {
                            break; //Without the break, there will not be any gaps!
                        }
                    }
                }
            }

            if (addLowConfNonTranslatedWords)
            {
                // 7) add non-translated words to the maximum.
                for (int i = 0; i < sourceSentenceTokens.Length; i++)
                {
                    if (!coveredSourceIdx.Contains(i))
                    {
                        int targetIdx = sourceMaxAlignments[i];

                        int maxIdx = -1;
                        double max = Double.MinValue;
                        for (int r = 0; r < alignmentMatrix.RowCount - 1; r++)
                        {
                            if (alignmentMatrix[r, i] > max && res[r].sourceIdList.Count < 1 && res[r].targetString != "UNK")
                            {
                                max = alignmentMatrix[r, i];
                                maxIdx = r;
                            }
                        }
                        if (maxIdx == -1)
                        {
                            for (int r = 0; r < alignmentMatrix.RowCount - 1; r++)
                            {
                                if (alignmentMatrix[r, i] > max)
                                {
                                    max = alignmentMatrix[r, i];
                                    maxIdx = r;
                                }
                            }
                        }

                        if (maxIdx >= 0)
                        {
                            if (res[maxIdx].sourceIdList.Count > 0)
                            {
                                int insertAt = 0;
                                for (int j = 0; j < res[maxIdx].sourceIdList.Count; j++)
                                {
                                    insertAt = j;
                                    if (res[maxIdx].sourceIdList[j] > i)
                                    {
                                        break;
                                    }
                                }
                                res[maxIdx].sourceIdList.Insert(insertAt, i);
                                res[maxIdx].sourceStrings.Insert(insertAt, sourceSentenceTokens[i]);
                            }
                            else
                            {
                                res[maxIdx].sourceIdList.Add(i);
                                res[maxIdx].sourceStrings.Add(sourceSentenceTokens[i]);
                            }
                            coveredSourceIdx.Add(i);
                        }
                    }
                }
            }
            
            foreach(NMTAlignmentElement elem in res)
            {
                elem.sourceString = string.Join(" ", elem.sourceStrings);
            }
            return res;
        }

        public int[] GetMaximum(Matrix<double> alignmentMatrix, bool calculateForRows)
        {
            int[] res = new int[calculateForRows ? alignmentMatrix.RowCount-1 : alignmentMatrix.ColumnCount-1];
            if (calculateForRows)
            {
                for (int r = 0; r < alignmentMatrix.RowCount-1; r++)
                {
                    int maxIdx = 0;
                    double max = Double.MinValue;
                    for (int c = 0; c < alignmentMatrix.ColumnCount-1; c++)
                    {
                        if (alignmentMatrix[r, c] > max)
                        {
                            max = alignmentMatrix[r, c];
                            maxIdx = c;
                        }
                    }
                    res[r] = maxIdx;
                }
            }
            else
            {
                for (int c = 0; c < alignmentMatrix.ColumnCount-1; c++)
                {
                    int maxIdx = 0;
                    double max = Double.MinValue;
                    for (int r = 0; r < alignmentMatrix.RowCount-1; r++)
                    {
                        if (alignmentMatrix[r, c] > max)
                        {
                            max = alignmentMatrix[r, c];
                            maxIdx = r;
                        }
                    }
                    res[c] = maxIdx;
                }
            }
            return res;
        }

        public static char[] sep = { ' ' };
        private static NumberFormatInfo nfi = new NumberFormatInfo();

        public Matrix<double> ReadAlignmentFile(string file)
        {
            string[] lines = File.ReadAllLines(file, Encoding.UTF8);
            return ReadAlignmentFile(lines);
        }

        public Matrix<double> ReadAlignmentFile(string[] lines)
        {
            if (lines != null && lines.Length > 0)
            {
                string[] colNumArr = lines[0].Split(sep, StringSplitOptions.RemoveEmptyEntries);
                Matrix<double> m = Matrix<double>.Build.Dense(lines.Length, colNumArr.Length);
                for (int row = 0; row < lines.Length; row++)
                {
                    double[] colArr = SplitLineInArr(lines[row]);
                    for (int col = 0; col < colNumArr.Length; col++)
                    {
                        m[row, col] = colArr[col];
                    }
                }
                return m;
            }
            else
            {
                return null;
            }
        }

        private double[] SplitLineInArr(string line)
        {
            string[] strArr = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            double[] res = new double[strArr.Length];
            for (int i = 0; i < strArr.Length; i++)
            {
                res[i] = Convert.ToDouble(strArr[i], nfi);
            }

            return res;
        }
    }


    public class NMTAlignmentElement
    {
        public List<string> sourceStrings = new List<string>();
        public List<string> targetStrings = new List<string>();
        public List<int> sourceIdList = new List<int>();
        public List<int> targetIdList = new List<int>();
        public string sourceString = null;
        public string targetString = null;
        public bool fixateTranslation = false;
    }
}
