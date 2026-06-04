using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

public static class SizeEvaluator
{
    public static double Evaluate(string expression, uint relative)
    {
        if (string.IsNullOrWhiteSpace(expression)) return 0;

        string Cleaned = expression.Replace(" ", "");

        string[] tokens = Regex.Split(Cleaned, @"([+\-*/])");

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();

            if (token == "+" || token == "-" || token == "*" || token == "/")
                continue;

            double pixelValue = ParseUnit(token, relative);

            tokens[i] = pixelValue.ToString(CultureInfo.InvariantCulture);
        }

        string mathExpression = string.Concat(tokens);

        return EvaluateMath(mathExpression);
    }

    private static double ParseUnit(string token, uint relative)
    {
        if (string.IsNullOrEmpty(token)) return 0;

        var match = Regex.Match(token, @"^(-?\d+\.?\d*)(px|%|vw|vh)?$");
        if (!match.Success)
        {
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double rawNum))
                return rawNum;

            throw new ArgumentException($"Invalid CSS size token: '{token}'");
        }

        double value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        string unit = match.Groups[2].Value.ToLower();

        return unit switch
        {
            "px" => value,
            "%" => value / 100.0 * relative,
            _ => value
        };
    }

    private static double EvaluateMath(string mathExpression)
    {
        try
        {
            using var table = new DataTable();
            var result = table.Compute(mathExpression, string.Empty);
            return Convert.ToDouble(result);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to evaluate math expression: '{mathExpression}'", ex);
        }
    }
}