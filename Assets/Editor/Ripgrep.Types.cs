using System.Collections.Generic;

namespace Editor.Tools.Ripgrep
{
    // used to extract just the type. The data object can be multiple types so its ignored here.
    [System.Serializable]
    struct JsonLine
    {
        public string type;
    }

    // type: begin
    [System.Serializable]
    public struct JsonBegin
    {
        public string type;
        public BeginData data;
    }

    [System.Serializable]
    public struct BeginData
    {
        public Text path;
    }

    // type: end
    [System.Serializable]
    public struct JsonEnd
    {
        public string type;
        public EndData EndData;
    }

    [System.Serializable]
    public struct EndData
    {
        public Text path;
        public object binary_offset;
        public Stats stats;
    }

    [System.Serializable]
    public struct Elapsed
    {
        public int secs;
        public int nanos;
        public string human;
    }

    [System.Serializable]
    public struct Stats
    {
        public Elapsed elapsed;
        public int searches;
        public int searches_with_match;
        public int bytes_searched;
        public int bytes_printed;
        public int matched_lines;
        public int matches;
    }

    // type: match
    [System.Serializable]
    public struct JsonMatch
    {
        public string type;
        public MatchData data;
    }

    [System.Serializable]
    public struct MatchData
    {
        public Text path;
        public Text lines;
        public int line_number;
        public int absolute_offset;
        public List<Submatch> submatches;
    }

    [System.Serializable]
    public struct Submatch
    {
        public Text match;
        public int start;
        public int end;
    }
    
    // type: summary
    [System.Serializable]
    public class JsonSummary
    {
        public SummaryData data;
        public string type;
    }
    
    [System.Serializable]
    public class SummaryData
    {
        public Elapsed elapsed_total;
        public Stats stats;
    }

    // Common text type

    [System.Serializable]
    public struct Text
    {
        public string text;
    }
}
