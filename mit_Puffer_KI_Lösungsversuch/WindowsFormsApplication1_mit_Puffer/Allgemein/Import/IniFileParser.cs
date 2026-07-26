using System;
using System.Collections.Generic;
using System.IO;

class IniFileParser
{
    private readonly Dictionary<string, Dictionary<string, string>> sections;

    public IniFileParser()
    {
        sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    }

    public void Parse(string filePath)
    {
        string currentSection = null;
        foreach (string line in File.ReadLines(filePath))
        {
            string trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
            {
                // Skip comments and empty lines
                continue;
            }

            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                // Found a new section
                currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                // Found key-value pair
                int equalsIndex = trimmedLine.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    string key = trimmedLine.Substring(0, equalsIndex).Trim();
                    string value = trimmedLine.Substring(equalsIndex + 1).Trim();
                    sections[currentSection][key] = value;
                }
            }
        }
    }

    public string GetValue(string section, string key)
    {
        if (sections.ContainsKey(section) && sections[section].ContainsKey(key))
        {
            return sections[section][key];
        }
        return null;
    }
}