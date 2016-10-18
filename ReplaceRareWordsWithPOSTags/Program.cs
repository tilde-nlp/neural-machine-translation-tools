using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplaceRareWordsWithPOSTags
{
    class Program
    {
        static void Main(string[] args)
        {
            string inFile = args[0];
            string outFile = args[1];
            int maxTokens = Convert.ToInt32(args[2]);
            maxTokens--; //As the last one will be UNK anyway!
            int maxPosTagLen = Convert.ToInt32(args[3]);
            string otherFile = args.Length > 4 ? args[4] : null;

            HashSet<string> posTags = new HashSet<string>();

            Dictionary<string, int> tokenDict = new Dictionary<string, int>();
            StreamReader sr = new StreamReader(inFile, Encoding.UTF8);

            char[] sep = { ' ' };
            char[] sep2 = { '|' };
            Console.WriteLine("Performing first pass through data:");
            int counter = 0;
            //Perform first pass through the data and count all tokens.
            while (!sr.EndOfStream)
            {
                counter++;
                if (counter%2000==0)
                {
                    Console.Write(".");
                    if (counter % 100000==0) Console.WriteLine(" - " + counter.ToString());
                }
                string line = sr.ReadLine().Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    string[] tokens = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string t in tokens)
                    {
                        string []parts = t.Split(sep2,StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length>=3)
                        {
                            string tag = parts[parts.Length - 1];
                            if (tag.Length>maxPosTagLen)
                            {
                                tag = tag.Substring(0, maxPosTagLen);
                            }
                            if (!posTags.Contains(tag)) posTags.Add(tag);
                        }

                        if (!tokenDict.ContainsKey(parts[0])) tokenDict.Add(parts[0], 1);
                        else tokenDict[parts[0]]++;
                    }
                }
            }
            sr.Close();

            //Sort tokens
            List<KeyValuePair<string, int>> myList = tokenDict.ToList();

            myList.Sort((firstPair, nextPair) => { return nextPair.Value.CompareTo(firstPair.Value); });
            for (int i=0;i<myList.Count;i++)
            {
                tokenDict[myList[i].Key] = i;
            }
            counter = 0;

            sr = new StreamReader(otherFile == null ? inFile : otherFile, Encoding.UTF8);
            StreamWriter sw = new StreamWriter(outFile, false, new UTF8Encoding(false));
            sw.NewLine = "\n";
            //Perform a second pass through the data and check whether a token should be replaced by its POS-tag.
            Console.WriteLine("\nPerforming second pass through data:");
            while (!sr.EndOfStream)
            {
                counter++;
                if (counter % 2000 == 0)
                {
                    Console.Write(".");
                    if (counter % 100000 == 0) Console.WriteLine(" - " + counter.ToString());
                }
                bool written = false;
                string line = sr.ReadLine().Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    string[] tokens = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string t in tokens)
                    {
                        string[] parts = t.Split(sep2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            if (written) sw.Write(" ");
                            written = true;
                            if (tokenDict.ContainsKey(parts[0]) && tokenDict[parts[0]] < maxTokens - posTags.Count)
                            {
                                //The token can be written
                                sw.Write(SplitMorphParts(parts[0]));
                            }
                            else
                            {
                                string tag = parts[parts.Length - 1];
                                if (tag.Length > maxPosTagLen)
                                {
                                    tag = tag.Substring(0, maxPosTagLen);
                                }
                                sw.Write(tag);
                            }
                        }
                        else
                        {
                            throw new InvalidDataException("Something is wrong with the factored data!");
                        }
                    }
                }
                sw.WriteLine();
            }
            Console.WriteLine("Count of POS-tags: " + posTags.Count);
            Console.WriteLine("\nDone!\n");
            sr.Close();
            sw.Close();
        }

        private static char[] underlineSeparator = { '_' };

        private static string SplitMorphParts(string word)
        {
            if (word.Contains('_')&&word[0]!='_'&&word[word.Length-1]!='_')
            {
                return string.Join(" ", word.Split(underlineSeparator, StringSplitOptions.RemoveEmptyEntries));
            }
            return word;
        }
    }
}
