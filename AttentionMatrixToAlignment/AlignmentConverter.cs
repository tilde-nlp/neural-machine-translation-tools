using MathNet.Numerics.LinearAlgebra;
using ProcessNMTAlignments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AttentionMatrixToAlignment
{
    class AlignmentConverter
    {
        /// <summary>
        /// A slow prototype to test alignment extraction,
        /// that breaks up many-to-many alignments by removing the least probable
        /// and leavs only one-to-many or many-to-one alignments
        /// </summary>
        /// <param name="alignmentMatrix"></param>
        /// <returns></returns>
        public static string GetAlignment(Matrix<double> alignmentMatrix)
        {
            List<Alignment> alignments = new List<Alignment>();
            double probabilityThreshold = 0.01;
            for (int rowCounter = 0; rowCounter < alignmentMatrix.RowCount - 1; rowCounter++)
            {
                for (int columnCounter = 0; columnCounter < alignmentMatrix.ColumnCount - 1; columnCounter++)
                {
                    if (alignmentMatrix[rowCounter, columnCounter] > probabilityThreshold)
                    {
                        alignments.Add(new Alignment { SourceToken = columnCounter, TargetToken = rowCounter, Probability = alignmentMatrix[rowCounter, columnCounter] });
                    }
                }
            }

            var clusters = FindClusters(alignments.ToList());//ToList() is intended, we need a second copy of teh list

            SimplifyClusters(clusters, alignments);
            return string.Join(" ", alignments.OrderBy(item => item.TargetToken).ThenBy(item => item.SourceToken).Select(item => item.SourceToken.ToString() + "-" + item.TargetToken.ToString()));
        }


        class Alignment : IComparable<Alignment>
        {
            public int TargetToken;
            public int SourceToken;
            public double Probability;

            int IComparable<Alignment>.CompareTo(Alignment other)
            {
                if (other.Probability > this.Probability)
                    return -1;
                else if (other.Probability == this.Probability)
                    return 0;
                else
                    return 1;
            }
        }

        private static List<List<Alignment>> FindClusters(List<Alignment> alignments)
        {
            List<List<Alignment>> clusters = new List<List<Alignment>>();
            while (true)
            {
                List<Alignment> currentCluster = new List<Alignment>();
                FindCluster(currentCluster, alignments);
                clusters.Add(currentCluster);
                if (!alignments.Any())
                {
                    return clusters;
                }
            }
        }

        private static void FindCluster(List<Alignment> currentCluster, List<Alignment> alignments)
        {
            if (alignments.Any())
            {
                var firstAlignment = alignments.First();
                FindClusterSource(currentCluster, alignments, firstAlignment.SourceToken);
            }
        }

        private static void FindClusterSource(List<Alignment> currentCluster, List<Alignment> alignments, int sourceToken)
        {
            var foundItems = alignments.Where(alignment => alignment.SourceToken == sourceToken).ToList();
            foreach (var alignment in foundItems)
            {
                currentCluster.Add(alignment);
                alignments.Remove(alignment);
            }
            foreach (var alignment in foundItems)
            {
                FindClusterTarget(currentCluster, alignments, alignment.TargetToken);
            }
        }

        private static void FindClusterTarget(List<Alignment> currentCluster, List<Alignment> alignments, int targetToken)
        {
            var foundItems = alignments.Where(alignment => alignment.TargetToken == targetToken).ToList();
            foreach (var alignment in foundItems)
            {
                currentCluster.Add(alignment);
                alignments.Remove(alignment);
            }
            foreach (var alignment in foundItems)
            {
                FindClusterSource(currentCluster, alignments, alignment.SourceToken);
            }
        }

        private static void SimplifyClusters(List<List<Alignment>> clusters, List<Alignment> globalAlignmentList)
        {
            foreach (var cluster in clusters)
            {
                SimplifyCluster(cluster, globalAlignmentList);
            }
        }

        private static void SimplifyCluster(List<Alignment> cluster, List<Alignment> globalAlignmentList)
        {
            //TODO: optmize
            var sourceDuplicates = cluster
                .GroupBy(alignment => alignment.SourceToken)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);
            var targetDuplicates = cluster
                .GroupBy(alignment => alignment.TargetToken)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);
            if (sourceDuplicates.Any() && targetDuplicates.Any())
            {
                //allright, this cluster has many-to-many relations
                //we want to simplify it down to one-to-many or many-to-one
                //so we drop alignments with lowest probability

                Alignment leastProbableAlignment = null;
                //but start with the worst offenders
                leastProbableAlignment = cluster
                    .Where(alignment => sourceDuplicates.Contains(alignment.SourceToken) && targetDuplicates.Contains(alignment.TargetToken))
                    .Min();
                if (leastProbableAlignment == null)
                {
                    leastProbableAlignment = cluster.Min();
                }

                cluster.Remove(leastProbableAlignment);
                globalAlignmentList.Remove(leastProbableAlignment);

                //now, see if what's left is broken into new clusters
                //and if those new clusters need to be simplified further
                SimplifyClusters(FindClusters(cluster), globalAlignmentList);
            }
        }

    }
}
