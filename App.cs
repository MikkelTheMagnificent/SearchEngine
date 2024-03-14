using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared;

namespace Indexer
{
    public class App
    {
        public void Run(){
            Database db = new Database();
            Crawler crawler = new Crawler(db);

            var root = new DirectoryInfo(Paths.FOLDER);

            DateTime start = DateTime.Now;

            crawler.IndexFilesIn(root, new List<string> { ".txt"});        

            TimeSpan used = DateTime.Now - start;
            Console.WriteLine("DONE! used " + used.TotalMilliseconds);

            var all = db.GetAllWords();
            // var wordOccurance = db.GetWordOccurrences();

            Console.WriteLine($"Indexed {db.GetDocumentCounts()} documents");
            Console.WriteLine($"Number of different words: {all.Count}");

            ////////

            Console.WriteLine("How many words would you like to see?"); 
            if (!int.TryParse(Console.ReadLine(), out int wordCount)) 
            {
                wordCount = 10;
            }

            var sortedWords = crawler.wordFrequencies.OrderByDescending(kvp => kvp.Value).ToList();

            for (int i = 0; i < sortedWords.Count && i < wordCount; i++)
            {
                Console.WriteLine($"<{sortedWords[i].Key}, {sortedWords[i].Value}> - {crawler.GetWordFrequency(sortedWords[i].Key)}");
            }

            ///////

            //Console.WriteLine($"Number of word occurances: {wordOccurance.Count}");
            int count = 10;

            Console.WriteLine($"The first {count} is:");
            foreach (var p in all) {

                string word = p.Key;
                int wordId = p.Value;
                int frequency = crawler.GetWordFrequency(word);

                Console.WriteLine($"<{word}, {wordId} - {frequency}");
                count--;
                if (count == 0) break;
            }
        }
    }
}
