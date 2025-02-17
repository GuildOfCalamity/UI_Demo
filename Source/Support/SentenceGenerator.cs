using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace UI_Demo;

/// <summary>
/// This is currently in development and requires attention.
/// </summary>
public class SentenceGenerator
{
    static readonly string[] Subjects = { "the cat", "a dog", "she", "he", "the scientist", "an artist", "the teacher", "the student" };
    static readonly string[] Objects = { "the book", "a cake", "the ball", "a letter", "the chair", "a song", "a masterpiece" };
    static readonly string[] Adjectives = { "happy", "angry", "excited", "sleepy", "brilliant", "curious", "brave", "lazy" };
    static readonly string[] Adverbs = { "furiously", "quickly", "slowly", "carefully", "elegantly", "silently", "enthusiastically", "gracefully", "angrily" };
    static readonly string[] Conjunctions = { "and then", "but", "so", "because", "while", "although" };
    static readonly string[] QuestionStarters = { "Why", "When", "How", "Where", "Who" };
    static readonly Dictionary<string, (string past, string future, string pastPassive, string futurePassive)> VerbConjugations = new()
    {
        { "eat", ("ate", "will eat", "was eaten", "will be eaten") },
        { "run", ("ran", "will run", "was run", "will be run") },
        { "jump", ("jumped", "will jump", "was jumped", "will be jumped") },
        { "kick", ("kicked", "will kick", "was kicked", "will be kicked") },
        { "sleep", ("slept", "will sleep", "was slept", "will be slept") },
        { "write", ("wrote", "will write", "was written", "will be written") },
        { "draw", ("drew", "will draw", "was drawn", "will be drawn") },
        { "teach", ("taught", "will teach", "was taught", "will be taught") },
        { "read", ("read", "will read", "was read", "will be read") },
        // "swam" is the past tense of swim, while "swum" is the past participle
        { "swim", ("swum", "will swim", "was swum", "will be swam") },
        { "create", ("created", "will create", "was created", "will be created") }
    };

    /// <summary>
    /// Testing method for the <see cref="SentenceGenerator"/> class.
    /// </summary>
    public static void RunTest()
    {
        // "The curious dog quickly eats a cake."
        Debug.WriteLine(GenerateSentence(VerbTense.Present, SentencePattern.Simple));

        // "The sleepy artist elegantly drew a masterpiece, but the brilliant teacher quickly read the book."
        Debug.WriteLine(GenerateSentence(VerbTense.Past, SentencePattern.Compound));

        // "When does the brave scientist gracefully create a masterpiece?"
        Debug.WriteLine(GenerateSentence(VerbTense.Future, SentencePattern.Interrogative));

        // "If the happy cat quickly ate a cake, then the brilliant student slowly read the book."
        Debug.WriteLine(GenerateSentence(VerbTense.Past, SentencePattern.Conditional));

        // "The sleepy dog did not carefully run the ball."
        Debug.WriteLine(GenerateSentence(VerbTense.Present, SentencePattern.Negative));

        // "The book was written by the scientist."
        Debug.WriteLine(GenerateSentence(VerbTense.Past, SentencePattern.Passive));
    }

    public static string GenerateSentence(VerbTense tense, SentencePattern pattern)
    {
        string sentence = pattern switch
        {
            SentencePattern.Simple => GenerateSimpleSentence(tense),
            SentencePattern.Compound => GenerateCompoundSentence(tense),
            SentencePattern.Interrogative => GenerateInterrogativeSentence(tense),
            SentencePattern.Conditional => GenerateConditionalSentence(tense),
            SentencePattern.Negative => GenerateNegativeSentence(tense),
            SentencePattern.Passive => GeneratePassiveSentence(tense),
            _ => GenerateSimpleSentence(tense)
        };
        return CapitalizeFirstLetter(sentence);
    }

    static string GetRandom(string[] words) => words[Random.Shared.Next(words.Length)];

    static string GenerateSimpleSentence(VerbTense tense)
    {
        string subject = GetFormattedSubject();
        string verb = GetRandomVerb(tense);
        string adverb = GetRandom(Adverbs);
        string obj = GetFormattedObject();

        return $"{subject} {adverb} {verb} {obj}.";
    }

    static string GenerateCompoundSentence(VerbTense tense)
    {
        string sentence1 = GenerateSimpleSentence(tense);
        string conjunction = GetRandom(Conjunctions);
        string sentence2 = GenerateSimpleSentence(tense).ToLower();

        return $"{sentence1} {conjunction}, {sentence2}";
    }

    static string GenerateInterrogativeSentence(VerbTense tense)
    {
        string questionStarter = GetRandom(QuestionStarters);
        string subject = GetFormattedSubject();
        string verb = GetRandomVerb(tense);
        string obj = GetFormattedObject();

        return $"{questionStarter} does {subject} {verb} {obj}?";
    }

    static string GenerateConditionalSentence(VerbTense tense)
    {
        string subject1 = GetFormattedSubject();
        string verb1 = GetRandomVerb(tense);
        string obj1 = GetFormattedObject();

        string subject2 = GetFormattedSubject();
        string verb2 = GetRandomVerb(tense);
        string obj2 = GetFormattedObject();

        return $"If {subject1} {verb1} {obj1}, then {subject2} {verb2} {obj2}.";
    }

    static string GenerateNegativeSentence(VerbTense tense)
    {
        string subject = GetFormattedSubject();
        string verb = GetRandomVerb(tense);
        string obj = GetFormattedObject();

        return $"{subject} did not {verb} {obj}.";
    }

    static string GeneratePassiveSentence(VerbTense tense)
    {
        string obj = GetFormattedObject();
        string subject = GetFormattedSubject();
        string verb = GetRandomPassiveVerb(tense);

        return $"{obj} {verb} by {subject}.";
    }

    static string GetFormattedSubject()
    {
        string subject = GetRandom(Subjects);
        string adjective = GetRandom(Adjectives);
        return subject.StartsWith("the") || subject.StartsWith("a") || subject.StartsWith("an")
            ? $"{subject.Split(' ')[0]} {adjective} {subject.Split(' ', 2)[1]}"  // Keep article, add adjective
            : $"{adjective} {subject}";  // No article, just add adjective
    }

    static string GetFormattedObject()
    {
        string obj = GetRandom(Objects);
        string adjective = GetRandom(Adjectives);
        return obj.StartsWith("the") || obj.StartsWith("a") || obj.StartsWith("an")
            ? $"{obj.Split(' ')[0]} {adjective} {obj.Split(' ', 2)[1]}"
            : $"{adjective} {obj}";
    }

    static string GetRandomVerb(VerbTense tense)
    {
        var verbPair = GetRandomPair(VerbConjugations);
        return tense switch
        {
            VerbTense.Present => verbPair.Key,
            VerbTense.Past => verbPair.Value.past,
            VerbTense.Future => verbPair.Value.future,
            _ => verbPair.Key
        };
    }

    static string GetRandomPassiveVerb(VerbTense tense)
    {
        var verbPair = GetRandomPair(VerbConjugations);
        return tense switch
        {
            VerbTense.Past => verbPair.Value.pastPassive,
            VerbTense.Future => verbPair.Value.futurePassive,
            _ => verbPair.Value.pastPassive
        };
    }

    static KeyValuePair<string, (string past, string future, string pastPassive, string futurePassive)> GetRandomPair(Dictionary<string, (string past, string future, string pastPassive, string futurePassive)> dict)
    {
        int index = Random.Shared.Next(dict.Count);
        foreach (var pair in dict)
        {
            if (index-- == 0) 
                return pair;
        }
        return default;
    }

    static string CapitalizeFirstLetter(string sentence)
    {
        if (string.IsNullOrEmpty(sentence)) 
            return sentence;
        return char.ToUpper(sentence[0], CultureInfo.CurrentCulture) + sentence.Substring(1);
    }
}

public enum VerbTense { Present, Past, Future }
public enum SentencePattern { Simple, Compound, Interrogative, Conditional, Negative, Passive }

/// <summary>
/// This is currently in development and requires attention.
/// </summary>
public class SentenceGeneratorAlt
{
    static readonly Random _random = new Random();
    static readonly string[] NounSubjects = { "the cat", "a dog", "the scientist", "an artist", "the teacher", "the student" };
    static readonly string[] PronounSubjects = { "she", "he", "they" };
    static readonly string[] Objects = { "the book", "a cake", "the ball", "a letter", "the chair", "a song", "a masterpiece" };
    static readonly string[] Adjectives = { "happy", "angry", "excited", "sleepy", "brilliant", "curious", "brave", "lazy" };
    static readonly string[] QuestionStarters = { "Why", "When", "How", "Where", "Who" };
    static readonly Dictionary<string, (string past, string future, string pastPassive, string futurePassive, string[] validObjects)> VerbConjugations = new()
    {
        { "eat", ("ate", "will eat", "was eaten", "will be eaten", new[] { "a cake" }) },
        { "run", ("ran", "will run", "was run", "will be run", new[] { "on the field" }) },
        { "jump", ("jumped", "will jump", "was jumped", "will be jumped", new[] { "over the hurdle" }) },
        { "write", ("wrote", "will write", "was written", "will be written", new[] { "a letter", "a book" }) },
        { "read", ("read", "will read", "was read", "will be read", new[] { "a book", "a letter" }) }
    };

    /// <summary>
    /// Testing method for the <see cref="SentenceGenerator"/> class.
    /// </summary>
    public static void RunTest()
    {
        // "The curious dog quickly eats a cake."
        Debug.WriteLine(GenerateSentence(VerbTense.Present, SentencePattern.Simple));

        // "The sleepy artist elegantly drew a masterpiece, but the brilliant teacher quickly read the book."
        Debug.WriteLine(GenerateSentence(VerbTense.Past, SentencePattern.Compound));

        // "When does the brave scientist gracefully create a masterpiece?"
        Debug.WriteLine(GenerateSentence(VerbTense.Future, SentencePattern.Interrogative));

        // "If the happy cat quickly ate a cake, then the brilliant student slowly read the book."
        Debug.WriteLine(GenerateSentence(VerbTense.Past, SentencePattern.Conditional));

        // "The sleepy dog did not carefully run the ball."
        Debug.WriteLine(GenerateSentence(VerbTense.Present, SentencePattern.Negative));

        // "The book was written by the scientist."
        Debug.WriteLine(GenerateSentence(VerbTense.Past, SentencePattern.Passive));
    }

    public static string GenerateSentence(VerbTense tense, SentencePattern pattern)
    {
        string sentence = pattern switch
        {
            SentencePattern.Simple => GenerateSimpleSentence(tense),
            SentencePattern.Compound => GenerateCompoundSentence(tense),
            SentencePattern.Interrogative => GenerateInterrogativeSentence(tense),
            SentencePattern.Conditional => GenerateConditionalSentence(tense),
            SentencePattern.Negative => GenerateNegativeSentence(tense),
            SentencePattern.Passive => GeneratePassiveSentence(tense),
            _ => GenerateSimpleSentence(tense)
        };

        return CapitalizeFirstLetter(sentence);
    }

    static string GenerateNegativeSentence(VerbTense tense)
    {
        string subject = GetFormattedSubject();
        var (verb, validObjects) = GetRandomVerb(tense);
        string obj = GetValidObject(validObjects);

        return $"{subject} did not {verb} {obj}.";
    }

    static string GeneratePassiveSentence(VerbTense tense)
    {
        string obj = GetValidObject(VerbConjugations["write"].validObjects);
        string subject = GetFormattedSubject();
        string verb = tense == VerbTense.Past ? "was written" : "will be written";

        return $"{obj} {verb} by {subject}.";
    }

    private static string GenerateSimpleSentence(VerbTense tense)
    {
        string subject = GetFormattedSubject();
        var (verb, validObjects) = GetRandomVerb(tense);
        string obj = GetValidObject(validObjects);

        return $"{subject} {verb} {obj}.";
    }

    private static string GenerateInterrogativeSentence(VerbTense tense)
    {
        string questionStarter = GetRandom(QuestionStarters);
        string subject = GetFormattedSubject();
        var (verb, _) = GetRandomVerb(tense);

        // Fix verb auxiliary rules
        string auxVerb = tense switch
        {
            VerbTense.Present => "does",
            VerbTense.Past => "did",
            VerbTense.Future => "will",
            _ => "does"
        };

        // Ensure correct verb form (no past/future after "does"/"did")
        string fixedVerb = (tense == VerbTense.Present || tense == VerbTense.Past) ? ConvertToBaseForm(verb) : verb;

        return $"{questionStarter} {auxVerb} {subject} {fixedVerb}?";
    }

    private static string GenerateCompoundSentence(VerbTense tense)
    {
        string sentence1 = GenerateSimpleSentence(tense);
        string sentence2 = GenerateSimpleSentence(tense).ToLower();
        return $"{sentence1}, but {sentence2}";
    }

    private static string GenerateConditionalSentence(VerbTense tense)
    {
        string subject1 = GetFormattedSubject();
        var (verb1, validObjects1) = GetRandomVerb(tense);
        string obj1 = GetValidObject(validObjects1);

        string subject2 = GetFormattedSubject();
        var (verb2, validObjects2) = GetRandomVerb(tense);
        string obj2 = GetValidObject(validObjects2);

        return $"If {subject1} {verb1} {obj1}, then {subject2} {verb2} {obj2}.";
    }

    private static string ConvertToBaseForm(string verb)
    {
        return verb switch
        {
            "ate" => "eat",
            "ran" => "run",
            "jumped" => "jump",
            "wrote" => "write",
            "read" => "read", // Present and past spellings are the same
            _ => verb
        };
    }

    private static string GetFormattedSubject()
    {
        bool useNoun = _random.Next(2) == 0;
        string subject = useNoun ? GetRandom(NounSubjects) : GetRandom(PronounSubjects);
        string adjective = useNoun ? GetRandom(Adjectives) : ""; // Only nouns get adjectives
        return useNoun ? $"{subject.Split(' ')[0]} {adjective} {subject.Split(' ', 2)[1]}" : subject;
    }

    private static (string, string[]) GetRandomVerb(VerbTense tense)
    {
        var verbPair = GetRandomPair(VerbConjugations);
        string verb = tense switch
        {
            VerbTense.Present => verbPair.Key,
            VerbTense.Past => verbPair.Value.past,
            VerbTense.Future => verbPair.Value.future,
            _ => verbPair.Key
        };
        return (verb, verbPair.Value.validObjects);
    }

    private static string GetValidObject(string[] validObjects)
    {
        return GetRandom(validObjects);
    }

    private static KeyValuePair<string, (string past, string future, string pastPassive, string futurePassive, string[] validObjects)> GetRandomPair(Dictionary<string, (string past, string future, string pastPassive, string futurePassive, string[] validObjects)> dict)
    {
        int index = _random.Next(dict.Count);
        foreach (var pair in dict)
        {
            if (index-- == 0) return pair;
        }
        return default;
    }

    static string GetRandom(string[] words) => words[_random.Next(words.Length)];

    static string CapitalizeFirstLetter(string sentence) => char.ToUpper(sentence[0], CultureInfo.CurrentCulture) + sentence.Substring(1);
}


