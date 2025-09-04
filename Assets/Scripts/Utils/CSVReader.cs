using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

public class CSVReader
{
	static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
	static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
	static char[] TRIM_CHARS = { '\"' };

	public static List<Dictionary<string, object>> ParseText(string text)
	{
		var list = new List<Dictionary<string, object>>();

		var lines = Regex.Split(text, LINE_SPLIT_RE);

		if (lines.Length <= 1) return list;

		var header = Regex.Split(lines[0], SPLIT_RE);
		for (var i = 1; i < lines.Length; i++)
		{

			var values = Regex.Split(lines[i], SPLIT_RE);
			if (values.Length == 0 || values[0] == "") continue;

			var entry = new Dictionary<string, object>();
			for (var j = 0; j < header.Length && j < values.Length; j++)
			{
				string value = values[j];
				value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
				object finalvalue = value;
				int n;
				float f;
				if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out n))
				{
					finalvalue = n;
				}
				else if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out f))
				{
					finalvalue = f;
				}
				entry[header[j]] = finalvalue;
			}
			list.Add(entry);
		}
		return list;
	}

	public static List<Dictionary<string, object>> ReadFromResources(string file)
	{
		TextAsset data = Resources.Load(file) as TextAsset;

		return ParseText(data.text);
	}
}