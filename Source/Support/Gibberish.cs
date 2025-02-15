using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI_Demo;

public static class Gibberish
{
    const int HistoryLimit = 5; // make sure this is less than the prefab amount
    static readonly Queue<string> _recentSentences = new();

    public static string GenerateSentence()
    {
        string sentence = string.Empty;
        do
        {
            sentence = GeneratePickupSentence();
        }
        while (IsSimilarToRecent(sentence));

        // Add to history and remove oldest if over limit
        _recentSentences.Enqueue(sentence);
        if (_recentSentences.Count > HistoryLimit)
        {
            _recentSentences.Dequeue();
        }

        return sentence;
    }

    /// <summary>
    /// Generates technical gibberish.
    /// </summary>
    static string GenerateTechnicalSentence(int wordCount)
    {
        string[] table = { "a", "server", "or", "workstation", "PC", "is", "technological", "technology", "power",
        "system", "used", "for", "diagnosing", "and", "analyzing", "data", "for", "reporting", "to", "on",
        "user", "monitor", "display", "interaction", "electric", "batteries", "along", "with", "some", "over",
        "under", "memory", "once", "in", "while", "special", "object", "can be", "found", "inside", "the",
        "HD", "SSD", "USB", "CDROM", "NVMe", "GPU", "RAM", "NIC", "RAID", "SQL", "API", "XML", "JSON", "website",
        "at", "cluster", "fiber-optic", "floppy-disk", "media", "storage", "Windows", "operating", "root",
        "database", "access", "denied", "granted", "file", "files", "folder", "folders", "directory", "path",
        "registry", "policy", "wire", "wires", "serial", "parallel", "bus", "fast", "slow", "speed", "bits",
        "bytes", "voltage", "current", "resistance", "wattage", "circuit", "inspection", "measurement", "continuity",
        "diagram", "specifications", "robotics", "telecommunication", "applied", "internet", "science", "code",
        "password", "username", "wireless", "digital", "headset", "programming", "framework", "enabled", "disabled",
        "timer", "information", "keyboard", "mouse", "printer", "peripheral", "binary", "hexadecimal", "network",
        "router", "mainframe", "host", "client", "software", "version", "format", "upload", "download", "login",
        "logout", "embedded", "barcode", "driver", "image", "document", "flow", "layout", "uses", "configuration" };

        string word = string.Empty;
        StringBuilder builder = new StringBuilder();
        // Select a random word from the array until word count is satisfied.
        for (int i = 0; i < wordCount; i++)
        {
            string tmp = table[Random.Shared.Next(table.Length)];

            if (wordCount < table.Length)
                while (word.Equals(tmp) || builder.ToString().Contains(tmp)) { tmp = table[Random.Shared.Next(table.Length)]; }
            else
                while (word.Equals(tmp)) { tmp = table[Random.Shared.Next(table.Length)]; }

            builder.Append(tmp).Append(' ');
            word = tmp;
        }
        string sentence = builder.ToString().Trim() + ". ";
        // Set the first letter of the first word in the sentence to uppercase.
        sentence = char.ToUpper(sentence[0]) + sentence.Substring(1);
        return sentence;
    }

    /// <summary>
    /// This is a simplified version of my pickup line generator.
    /// </summary>
    /// <returns>jibber-jabber</returns>
    static string GeneratePickupSentence(bool prefab = true)
    {
        /*
        o----------------------------------------------------o
        |              BASIC SENTENCE ANATOMY                |
        o----------------------------------------------------o
                               Sentence
                                  /\
                                 /  \
                                /    \
                          Subject     \
                          /  \ 	      Predicate
                         /	  \		   /  \    \
                  Article  Object     /    \    \ 
                   /       /  \	    Verb    \    \
                "The"     /    \	  \    "the"  \
                         /    Noun     \         Object
                   Adjective   \ 	    \		   |
                      /		    \	  "kicked"     |
                     / 		  "bear"	          Noun
                "spicy"                            |
                                                "walrus"


             Final: "The spicy bear kicked the walrus."
        */

        string[] article = { "the", "one", "my", "this", "some", "a", /* "an" */ };
        string[] conjunction = { "and", "for", /* "so", "but", "or", "yet" */ };
        string[] superlative = { "biggest", "blackest", "boldest", "bravest", "brightest", "cheapest", "cleanest", "cleverest", "coldest", "dullest", "drunkest", "faintest", "fewest", "gentlest", "grandest", "gravest", "greatest", "highest", "kindest", "loudest", "moistest", "narrowest", "nicest", "oddest", "proudest", "purest", "quietest", "rarest", "richest", "ripest", "roughest", "rudest", "safest", "shallowest", "simplest", "smoothest", "strangest", "strictest", "truest", "weirdest", "youngest", };
        string[] adjective = { "steamy", "gentle", "grand", "cheap", "young", "old", "bright", "bold", "loud", "ripe", "powerful", "sticky", "spicy", "strange", "illegal", "crazy", "smelly", "wet", "bad", "hairy", "radiant", "meaningless", "nicer", "adorable", "part-time", "open-minded", "well-behaved", "cold-blooded", "beautiful", "breakable", "mathematical", "homeless", "wooden", "biological", "inedible", "incomprehensible", "inquisitive", "weird", };
        string[] noun = { "person", "cheese", "ham-bone", "garden", "dog", "town", "car", "container", "house", "bird", "hose", "horse", "statue", "game", "community", "team", "teacher", "room", "book", "job", "building", "spouse", "company", "student", "state", "world", "planet", "system", "service", "thing", "problem", "toothache", "hand", "part", "place", "alligator", "weasel", "animal", "moose", "ostrich", "cheetah", "monkey", "snake", "platypus", };
        string[] pastverb = { "toasted", "watched", "scuttled", "drove", "jumped", "ran", "walked", "skipped", "flew", "cranked", "barked", "sprayed", "owned", "dreamed", "asked", "accelerated", "soaked", "broke", "burned", "grew", "remained", "asked", "growled", "helped", "opened", "started", "buzzed", "kicked", "licked", "rolled", "laughed", "lived", "enjoyed", "needed", "fixed", "spilled", "yelled", "failed", "finished", "cooked", "talked", "stayed", "worked", "scratched", "showed", "barfed", "destroyed", "missed", "offered", "spied", "erupted", "sizzled", };
        string[] presentverb = { "toast", "watch", "boast", "scuttle", "drive", "jump", "run", "walk", "skip", "fly", "crank", "bark", "spray", "own", "dream", "ask", "accelerate", "soak", "brake", "burn", "grow", "remain", "ask", "growl", "help", "open", "start", "buzz", "punch", "kick", "lick", "roll", "laugh", "live", "enjoy", "need", "fix", "spill", "yell", "fail", "finish", "cook", "talk", "stay", "work", "scratch", "show", "destroy", "miss", "offer", "spy", "erupt", "sizzle", "swim", "barf", "thump" };
        string[] preposition = { "above", "across", "after", "at", "around", "by", "before", "behind", "below", "beside", "between", "during", "off of", "out of", "to", "from", "for", "over", "under", "on", "onto", "with", "inside", "through", "in", "up", "down" };
        string[] gerund = { "driving", "jumping", "running", "skipping", "flying", "cranking", "barking", "spraying", "dreaming", "asking", "breaking", "soaking", "burning", "growing", "asking", "growling", "helping", "opening", "starting", "buzzing", "punching", "kicking", "licking", "rolling", "laughing", "living", "enjoying", "needing", "fixing", "spilling", "yelling", "failing", "finishing", "cooking", "talking", "working", "scratching", "showing", "destroying", "missing", "spying", "erupting", "sizzling", "swimming", "barfing", "thumping", "falling" };

        #region [Random Statements]

        string sentence = string.Empty;
        if (Random.Shared.Next(10) > 5) // plain & non-spicy (40% chance)
            sentence = String.Format("{0} {1} {2} {3} {4} {5}", article[Random.Shared.Next(article.Length)], noun[Random.Shared.Next(noun.Length)], pastverb[Random.Shared.Next(pastverb.Length)], preposition[Random.Shared.Next(preposition.Length)], article[Random.Shared.Next(article.Length)], noun[Random.Shared.Next(noun.Length)]) + ".";
        else if (Random.Shared.Next(10) > 2) // sprinkle with adjectives (70% chance)
            sentence = String.Format("{0} {1} {2} {3} {4} {5} {6} {7}", article[Random.Shared.Next(article.Length)], adjective[Random.Shared.Next(adjective.Length)], noun[Random.Shared.Next(noun.Length)], pastverb[Random.Shared.Next(pastverb.Length)], preposition[Random.Shared.Next(preposition.Length)], article[Random.Shared.Next(article.Length)], adjective[Random.Shared.Next(adjective.Length)], noun[Random.Shared.Next(noun.Length)]) + ".";
        else // sprinkle with adjectives & conjunctions (10% chance)
            sentence = String.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}", article[Random.Shared.Next(article.Length)], adjective[Random.Shared.Next(adjective.Length)], noun[Random.Shared.Next(noun.Length)], pastverb[Random.Shared.Next(pastverb.Length)], preposition[Random.Shared.Next(preposition.Length)], article[Random.Shared.Next(article.Length)], adjective[Random.Shared.Next(adjective.Length)], noun[Random.Shared.Next(noun.Length)], conjunction[Random.Shared.Next(conjunction.Length)], article[Random.Shared.Next(article.Length)], noun[Random.Shared.Next(noun.Length)]) + ".";

        // Set the first letter of the first word in the sentence to uppercase
        sentence = char.ToUpper(sentence[0]) + sentence.Substring(1);

        #endregion

        #region [Prefabricated Statements]

        // Setup our lookup table of sentences
        string[] statements = {
        "I'm here for the [ADJ1] [NOUN1] convention, that's my business.",
        "I knew right away that my [NOUN1] and your [NOUN2] could be best friends.",
        "Never in my [SUPER1] dreams did I think I would've [PASTVERB1] a [NOUN1] like you.",
        "I'm going to [PRESVERB1] around my [ADJ1] [NOUN1] at the [NOUN2] tomorrow, so be ready!",
        "If you weren't so [ADJ1] you could be a [NOUN1].",
        "I'm not sure what's more beautiful, your [ADJ1] [NOUN1] or your [ADJ2] [NOUN2].",
        "I currently own the [SUPER1] [NOUN1] in the country.",
        "I found the [SUPER1] [ADJ1] [NOUN1], want to see it?",
        "Everyone says I've got the [SUPER1], most [ADJ1] [NOUN1] in town.",
        "Can you smell my [ADJ1] [NOUN1] from over there?",
        "Is there an airport nearby or is that just my [ADJ1] [NOUN1] taking off?",
        "Don't take this the wrong way, but you should [PRESVERB1] less.",
        "Forgive my [ADJ1] [NOUN1], but I couldn't help but notice your [ADJ2] [NOUN2].",
        "Did anybody happen to [PRESVERB1] on that [ADJ1] [NOUN1] last night?",
        "I'll [PRESVERB1] [PREP1] this [NOUN1] tomorrow, want to join me?",
        "I heard about your [ADJ1] [NOUN1], I hope it's still [ADJ2].",
        "A [NOUN1] can always be found [PREP1] [ADJ1] [NOUN2]s.",
        "Let's hop in my [ADJ1] [NOUN1] and go for a ride!",
        "Today is the day to get some [ADJ1] [NOUN1]s and go [GER1].",
        "I currently run a business that sells [ADJ1] [NOUN1]s.",
        "I'm starting to [PRESVERB1] [ADJ1] [NOUN1]s.",
        "I think I'll bring my [SUPER1], most [SUPER2] [NOUN1] to show & tell tomorrow.",
        "Last week I fell in love with a [ADJ1] [NOUN1] in Tijuana.",
        "It's going to freeze tonight so bring your [ADJ1] [NOUN1]s inside.",
        "Don't forget to check for [ADJ1] [NOUN1]s before going out tonight.",
        "I've updated my wardrobe to include [ADJ1] [NOUN1]s.",
        "We should get [PASTVERB1] and [PASTVERB2] immediately.",
        "I'm lost, can you give me directions to your [NOUN1]?",
        "Are you from Tennessee? Because you're the only [ADJ1] [NOUN1] I see!",
        "I'm like a Rubik's Cube, the more you [PRESVERB1] me, the more [ADJ1] I get.",
        "You sound like a [ADJ1] [NOUN1], so maybe you should [PRESVERB1] a little more.",
        "I'm going to be perfecting my [GER1] tomorrow, want to join me?",
        "[GER1] is my favorite way to exercise.",
        "I'm going to [PRESVERB1] my way to the top by [GER1] you over my [ADJ1] [NOUN1].",
        "I just made a huge investment in [ADJ1] [NOUN1]s, let's hope it pays off.",
        "I have some questions regarding the article you wrote about [ADJ1] [NOUN1]s that practice [GER1].",
        "I'm trading in my old car for some [ADJ1] [NOUN1]s.",
    };

        // Pick a sentence to use 
        string output = statements[Random.Shared.Next(statements.Length)];

        // Replace tags (up to 3 of each type)
        output = output.Replace("[ART1]", article[Random.Shared.Next(article.Length)]).Replace("[ART2]", article[Random.Shared.Next(article.Length)]).Replace("[ART3]", article[Random.Shared.Next(article.Length)]);
        output = output.Replace("[CONJ1]", conjunction[Random.Shared.Next(conjunction.Length)]).Replace("[CONJ2]", conjunction[Random.Shared.Next(conjunction.Length)]).Replace("[CONJ3]", conjunction[Random.Shared.Next(conjunction.Length)]);
        output = output.Replace("[SUPER1]", superlative[Random.Shared.Next(superlative.Length)]).Replace("[SUPER2]", superlative[Random.Shared.Next(superlative.Length)]).Replace("[SUPER3]", superlative[Random.Shared.Next(superlative.Length)]);
        output = output.Replace("[ADJ1]", adjective[Random.Shared.Next(adjective.Length)]).Replace("[ADJ2]", adjective[Random.Shared.Next(adjective.Length)]).Replace("[ADJ3]", adjective[Random.Shared.Next(adjective.Length)]);
        output = output.Replace("[NOUN1]", noun[Random.Shared.Next(noun.Length)]).Replace("[NOUN2]", noun[Random.Shared.Next(noun.Length)]).Replace("[NOUN3]", noun[Random.Shared.Next(noun.Length)]);
        output = output.Replace("[PRESVERB1]", presentverb[Random.Shared.Next(presentverb.Length)]).Replace("[PRESVERB2]", presentverb[Random.Shared.Next(presentverb.Length)]).Replace("[PRESVERB3]", presentverb[Random.Shared.Next(presentverb.Length)]);
        output = output.Replace("[PASTVERB1]", pastverb[Random.Shared.Next(pastverb.Length)]).Replace("[PASTVERB2]", pastverb[Random.Shared.Next(pastverb.Length)]).Replace("[PASTVERB3]", pastverb[Random.Shared.Next(pastverb.Length)]);
        output = output.Replace("[PREP1]", preposition[Random.Shared.Next(preposition.Length)]).Replace("[PREP2]", preposition[Random.Shared.Next(preposition.Length)]).Replace("[PREP3]", preposition[Random.Shared.Next(preposition.Length)]);
        output = output.Replace("[GER1]", gerund[Random.Shared.Next(gerund.Length)]).Replace("[GER2]", gerund[Random.Shared.Next(gerund.Length)]).Replace("[GER3]", gerund[Random.Shared.Next(gerund.Length)]);

        #endregion

        if (prefab)
            return output;
        else
            return sentence;
    }

    #region [Helpers]
    static bool IsSimilarToRecent(string newSentence)
    {
        return _recentSentences.Any(sentence => GetJaccardSimilarity(sentence, newSentence) >= 0.7);
    }

    static double GetJaccardSimilarity(string s1, string s2)
    {
        var set1 = new HashSet<string>(s1.Split(' '));
        var set2 = new HashSet<string>(s2.Split(' '));
        var intersection = set1.Intersect(set2).Count();
        var union = set1.Union(set2).Count();
        var score = (double)intersection / (double)union;
        Debug.WriteLine($"[INFO] Similarity score: {score:N3}");
        return score;
    }
    #endregion
}
