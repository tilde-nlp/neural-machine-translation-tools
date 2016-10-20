using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ProcessNMTAlignments;
using MathNet.Numerics.LinearAlgebra;
using System.Globalization;

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
                alignmentMatrixLines = alignmentMatrixLines.Select(theLine => theLine.Trim(new char[] { ' ', '(', ')' })).ToArray();
                Matrix<double> alignmentMatrix = Matrix<double>.Build.Dense(alignmentMatrixLines.Length, alignmentMatrixLines[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length);
                for(int rowIndex = 0; rowIndex< alignmentMatrixLines.Length; rowIndex++){
                    var values = alignmentMatrixLines[rowIndex].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for(int columnIndex = 0; columnIndex < values.Length; columnIndex++){
                        alignmentMatrix[rowIndex, columnIndex] = double.Parse(values[columnIndex], CultureInfo.InvariantCulture);
                    }
                }

                var fakeSourceTokens = Enumerable.Repeat<string>("", alignmentMatrix.ColumnCount - 1).ToArray();
                var fakeTargetTokens = Enumerable.Repeat<string>("", alignmentMatrix.RowCount - 1).ToArray();

                var alignments = new NMTAlignmentProcessor().GetMaxAlignments(fakeSourceTokens, fakeTargetTokens , alignmentMatrix);
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
                
                Console.WriteLine(inputParts[0] + " ||| " + string.Join(" ", flatAlignments.OrderBy(item=>item.Item2).ThenBy(item=>item.Item1).Select(item => item.Item1.ToString() + "-" + item.Item2.ToString())));
            }
        }
    }
}
