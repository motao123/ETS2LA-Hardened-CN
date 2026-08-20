using System.Reflection;

namespace ETS2LA.Game.Output;

public static class ControlMixer
{
    private static readonly FieldInfo[] FloatFields = typeof(ControlVariables)
        .GetFields(BindingFlags.Instance | BindingFlags.Public)
        .Where(field => field.FieldType == typeof(float?))
        .ToArray();

    public static IReadOnlyDictionary<string, float> Mix(IEnumerable<ControlChannel> channels)
    {
        var contributions = new Dictionary<string, List<WeightedValue>>(StringComparer.Ordinal);

        foreach (var channel in channels)
        {
            var weight = channel.Properties.Weight;
            if (!float.IsFinite(weight) || weight <= 0f)
                continue;

            foreach (var field in FloatFields)
            {
                if (field.GetValue(channel.Variables) is not float value || !float.IsFinite(value))
                    continue;

                var controlName = field.Name;
                if (controlName == "aforward")
                    controlName = "acceleration";
                else if (controlName == "abackward")
                {
                    controlName = "acceleration";
                    value = -value;
                }

                if (!contributions.TryGetValue(controlName, out var values))
                {
                    values = new List<WeightedValue>();
                    contributions[controlName] = values;
                }
                values.Add(new WeightedValue(weight, value));
            }
        }

        var result = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (var (controlName, values) in contributions)
        {
            if (TryMix(values, out var value))
                result[controlName] = value;
        }
        return result;
    }

    public static bool TryMix(IEnumerable<WeightedValue> values, out float result)
    {
        double weightedTotal = 0;
        double totalWeight = 0;

        foreach (var contribution in values)
        {
            if (!float.IsFinite(contribution.Weight) || contribution.Weight <= 0f ||
                !float.IsFinite(contribution.Value))
                continue;

            weightedTotal += contribution.Weight * (double)contribution.Value;
            totalWeight += contribution.Weight;
        }

        if (!double.IsFinite(weightedTotal) || !double.IsFinite(totalWeight) || totalWeight <= 0d)
        {
            result = 0f;
            return false;
        }

        var mixed = weightedTotal / totalWeight;
        if (!double.IsFinite(mixed))
        {
            result = 0f;
            return false;
        }

        result = (float)Math.Clamp(mixed, -1d, 1d);
        return true;
    }
}

public readonly record struct WeightedValue(float Weight, float Value);
