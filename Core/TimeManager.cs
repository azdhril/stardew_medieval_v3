using System;
using Microsoft.Xna.Framework;

namespace stardew_medieval_v3.Core;

/// <summary>
/// Central in-game clock. Advances from 6AM to 2AM, then triggers day advance.
/// </summary>
public class TimeManager
{
    public int DayNumber { get; private set; } = 1;
    public int Season { get; set; } // 0=Spring, 1=Summer, 2=Autumn, 3=Winter
    public float GameTime { get; private set; } // 0.0 = 6AM, 1.0 = 2AM

    public float DayDurationSeconds { get; set; } = 120f; // 2 minutes real time = 1 game day

    public event Action<float>? OnHourPassed;
    public event Action? OnDayAdvanced;

    private int _lastHour = 6;

    public int CurrentHour => 6 + (int)(GameTime * 20f); // 6AM to 26 (2AM)

    public void Update(float deltaTime)
    {
        GameTime += deltaTime / DayDurationSeconds;

        int hour = CurrentHour;
        if (hour != _lastHour)
        {
            _lastHour = hour;
            OnHourPassed?.Invoke(hour);
        }

        if (GameTime >= 1f)
        {
            AdvanceDay();
        }
    }

    public void ForceSleep()
    {
        AdvanceDay();
    }

    private void AdvanceDay()
    {
        GameTime = 0f;
        _lastHour = 6;
        DayNumber++;
        OnDayAdvanced?.Invoke();
    }

    public string GetDisplayHour()
    {
        int h = CurrentHour;
        if (h >= 24) h -= 24; // wrap 24-26 to 0-2

        int display = h % 12;
        if (display == 0) display = 12;
        string ampm = h < 12 ? "AM" : "PM";
        return $"{display}{ampm}";
    }

    public void SetDay(int day) => DayNumber = day;
    public void SetGameTime(float time) { GameTime = time; _lastHour = CurrentHour; }

    /// <summary>
    /// Returns light intensity multiplier based on time of day (0.0 to 1.0).
    /// </summary>
    public float GetLightIntensity()
    {
        // 6AM(0.0)->0.7, 9AM(0.15)->1.0, 2PM(0.4)->1.1, 7PM(0.65)->0.6, 10PM(0.8)->0.2, 2AM(1.0)->0.1
        float t = GameTime;
        if (t < 0.15f) return MathHelper.Lerp(0.7f, 1.0f, t / 0.15f);
        if (t < 0.4f) return MathHelper.Lerp(1.0f, 1.1f, (t - 0.15f) / 0.25f);
        if (t < 0.65f) return MathHelper.Lerp(1.1f, 0.6f, (t - 0.4f) / 0.25f);
        if (t < 0.8f) return MathHelper.Lerp(0.6f, 0.2f, (t - 0.65f) / 0.15f);
        return MathHelper.Lerp(0.2f, 0.1f, (t - 0.8f) / 0.2f);
    }

    public Microsoft.Xna.Framework.Color GetLightColor()
    {
        float intensity = MathHelper.Clamp(GetLightIntensity(), 0f, 1.1f);
        float t = GameTime;

        // Warm tones during sunrise/sunset
        float r = intensity;
        float g = intensity;
        float b = intensity;

        if (t < 0.1f) // Sunrise - warm orange
        {
            r = MathHelper.Min(r * 1.2f, 1f);
            b *= 0.8f;
        }
        else if (t > 0.55f && t < 0.75f) // Sunset - warm orange/purple
        {
            r = MathHelper.Min(r * 1.3f, 1f);
            b *= 0.85f;
        }
        else if (t > 0.8f) // Night - blue tint
        {
            r *= 0.7f;
            g *= 0.75f;
            b = MathHelper.Min(b * 1.3f, 0.4f);
        }

        return new Color(
            MathHelper.Clamp(r, 0, 1),
            MathHelper.Clamp(g, 0, 1),
            MathHelper.Clamp(b, 0, 1)
        );
    }
}
