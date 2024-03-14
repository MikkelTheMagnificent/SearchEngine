using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared.Model;

namespace Indexer
{
    public class Crawler
    {
        private readonly char[] separators = " \\\n\t\"$'!,?;.:-_**+=)([]{}<>/@&%€#".ToCharArray();
        /* Will be used to spilt text into words. So a word is a maximal sequence of
         * chars that does not contain any char from separators */
        
    
        private Dictionary<string, int> words = new Dictionary<string, int>();
        /* Will contain all words from files during indexing - thet key is the 
         * value of the word and the value is its id in the database */

        public Dictionary<string, int> wordFrequencies = new Dictionary<string, int>();

        private int documentCounter = 0;
        /* Will count the number of documents indexed during indexing */

        IDatabase mdatabase;

        public Crawler(IDatabase db){ mdatabase = db; }

        //Return a dictionary containing all words (as the key)in the file
        // [f] and the value is the number of occurrences of the key in file.
        private void ExtractWordsInFile(FileInfo f)
        {
            var content = File.ReadAllLines(f.FullName);
            foreach (var line in content)
            {
                foreach (var word in line.Split(separators, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (wordFrequencies.ContainsKey(word))
                    {
                        wordFrequencies[word]++;
                    }
                    else
                    {
                        wordFrequencies[word] = 1;
                    }
                }
            }
        }


        private ISet<int> GetWordIdFromWords(ISet<string> src)
        {
            ISet<int> res = new HashSet<int>();

            foreach (var word in src)
            {
                if (!words.ContainsKey(word))
                {
                    words[word] = words.Count + 1;
                }
                res.Add(words[word]);
            }
            return res;
        }

        // Return a dictionary of all the words (the key) in the files contained
        // in the directory [dir]. Only files with an extension in
        // [extensions] is read. The value part of the return value is
        // the number of occurrences of the key.
        public void IndexFilesIn(DirectoryInfo dir, List<string> extensions)
        {
            Console.WriteLine($"Crawling {dir.FullName}");

            foreach (var file in dir.EnumerateFiles())
            {
                if (extensions.Contains(file.Extension))
                {
                    documentCounter++;
                    BEDocument newDoc = new BEDocument
                    {
                        mId = documentCounter,
                        mUrl = file.FullName,
                        mIdxTime = DateTime.Now.ToString(),
                        mCreationTime = file.CreationTime.ToString()
                    };

                    mdatabase.InsertDocument(newDoc);

                    // Opdater ordforekomsterne her. Dette vil opdatere wordFrequencies direkte.
                    ExtractWordsInFile(file);

                    // Opbyg en ny ordbog med ordene og deres tildelte ID'er.
                    Dictionary<string, int> newWords = new Dictionary<string, int>();
                    foreach (var aWord in wordFrequencies.Keys)
                    {
                        if (!words.ContainsKey(aWord))
                        {
                            words.Add(aWord, words.Count + 1);
                            newWords.Add(aWord, words[aWord]);
                        }
                    }
                    mdatabase.InsertAllWords(newWords);

                    // Konverter KeyCollection til et HashSet, så det kan sendes til GetWordIdFromWords.
                    ISet<int> wordIds = GetWordIdFromWords(new HashSet<string>(wordFrequencies.Keys));
                    mdatabase.InsertAllOcc(newDoc.mId, wordIds);
                }
            }

            // Rekursivt kald til IndexFilesIn for at indeksere undermapper.
            foreach (var d in dir.EnumerateDirectories())
                IndexFilesIn(d, extensions);

            Console.WriteLine($"Indexed {documentCounter} documents");
            Console.WriteLine($"Number of different words: {words.Count}");

            // Sorter og udskriv de mest forekommende ord.
            var sortedWords = wordFrequencies.OrderByDescending(kvp => kvp.Value).ToList();
            for (int i = 0; i < sortedWords.Count && i < 10; i++)
            {
                Console.WriteLine($"<{sortedWords[i].Key}, {sortedWords[i].Value}> - {wordFrequencies[sortedWords[i].Key]}");
            }
        }

        public int GetWordFrequency(string word)
        {
            return wordFrequencies.ContainsKey(word) ? wordFrequencies[word] : 0;
        }


    }

}
