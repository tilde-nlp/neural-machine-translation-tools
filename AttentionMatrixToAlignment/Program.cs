using MathNet.Numerics.LinearAlgebra;
using ProcessNMTAlignments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AttentionMatrixToAlignment
{
    class Program
    {
        static void Main(string[] args)
        {
            string line = null;
            Console.SetIn(new StreamReader(Console.OpenStandardInput(65536), System.Text.UTF8Encoding.UTF8));
            Console.OutputEncoding = new System.Text.UTF8Encoding(false); // no BOM

            while ((line = Console.ReadLine()) != null)
            {
                var inputParts = line.Split(new string[] { " ||| " }, StringSplitOptions.None);
                var alignmentMatrixLines = inputParts[1].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                alignmentMatrixLines = alignmentMatrixLines
                    .Select(theLine => theLine.Trim(new char[] { ' ', '(', ')' }))
                    .Where(item=>item != string.Empty)
                    .ToArray();
                // ignore last line that corresponds to EOS
                int matrixLinesCount = alignmentMatrixLines.Length; 
                //ignore last column that corresponds to EOS
                int matrixColumnCount = alignmentMatrixLines[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                Matrix<double> alignmentMatrix = Matrix<double>.Build.Dense(matrixLinesCount, matrixColumnCount);
                for(int rowIndex = 0; rowIndex< matrixLinesCount; rowIndex++){
                    var values = alignmentMatrixLines[rowIndex].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for(int columnIndex = 0; columnIndex < matrixColumnCount; columnIndex++){
                        alignmentMatrix[rowIndex, columnIndex] = double.Parse(values[columnIndex], CultureInfo.InvariantCulture);
                    }
                }

                string processedAlignment = AlignmentByMarcis(alignmentMatrix);
                //string processedAlignment = AlignmentByToms(alignmentMatrix);

                Console.WriteLine(inputParts[0] + " ||| " + processedAlignment);
            }
        }

        static string AlignmentByToms(Matrix<double> alignmentMatrix)
        {
            return AlignmentConverter.GetAlignment(alignmentMatrix);
        }

        static string AlignmentByMarcis(Matrix<double> alignmentMatrix)
        {
            var fakeSourceTokens = Enumerable.Repeat<string>("", alignmentMatrix.ColumnCount - 1).ToArray();
            var fakeTargetTokens = Enumerable.Repeat<string>("", alignmentMatrix.RowCount - 1).ToArray();

            var alignments = new NMTAlignmentProcessor().GetMaxAlignments(fakeSourceTokens, fakeTargetTokens, alignmentMatrix, true);
            List<Tuple<int, int>> flatAlignments = new List<Tuple<int, int>>();
            foreach (var alignment in alignments)
            {
                foreach (var targetId in alignment.targetIdList)
                {
                    foreach (var sourceId in alignment.sourceIdList)
                    {
                        flatAlignments.Add(new Tuple<int, int>(sourceId, targetId));
                    }
                }
            }
            return string.Join(" ", flatAlignments.OrderBy(item => item.Item2).ThenBy(item => item.Item1).Select(item => item.Item1.ToString() + "-" + item.Item2.ToString()));
        }
    }
}
