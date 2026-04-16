using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace stardew_medieval_v3.UI;

/// <summary>
/// Gold particle burst spawned at the player's world position on level-up.
/// Renders in world-space (inside the camera-transformed SpriteBatch block).
/// Does NOT pause gameplay (D-08).
/// </summary>
public class LevelUpParticles
{
    private struct Particle
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public float Life;
        public float MaxLife;
    }

    private const float Gravity = 40f;
    private const int MinCount = 12;
    private const int MaxCount = 16;
    private const int ParticleSize = 3;

    private readonly List<Particle> _particles = new();
    private readonly Random _rng = new();

    /// <summary>
    /// Spawn a burst of gold particles at the given world position.
    /// </summary>
    /// <param name="worldPos">Center of the particle burst in world coordinates.</param>
    public void Spawn(Vector2 worldPos)
    {
        int count = _rng.Next(MinCount, MaxCount + 1);
        for (int i = 0; i < count; i++)
        {
            // Random outward velocity (magnitude 30-80)
            float angle = (float)(_rng.NextDouble() * Math.PI * 2.0);
            float speed = 30f + (float)_rng.NextDouble() * 50f;
            var vel = new Vector2(
                (float)Math.Cos(angle) * speed,
                (float)Math.Sin(angle) * speed);

            float life = 0.4f + (float)_rng.NextDouble() * 0.4f; // 0.4-0.8s

            _particles.Add(new Particle
            {
                Pos = worldPos,
                Vel = vel,
                Life = life,
                MaxLife = life,
            });
        }
    }

    /// <summary>
    /// Advance particle physics. Applies gravity and removes dead particles.
    /// </summary>
    public void Update(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Pos += p.Vel * dt;
            p.Vel.Y += Gravity * dt;
            p.Life -= dt;

            if (p.Life <= 0f)
            {
                _particles.RemoveAt(i);
            }
            else
            {
                _particles[i] = p;
            }
        }
    }

    /// <summary>
    /// Draw particles in world-space. Caller must have SpriteBatch.Begin with camera transform.
    /// </summary>
    /// <param name="sb">Active SpriteBatch in world-space.</param>
    /// <param name="pixel">1x1 white pixel texture.</param>
    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        foreach (var p in _particles)
        {
            float alpha = p.Life / p.MaxLife;
            var rect = new Rectangle((int)p.Pos.X, (int)p.Pos.Y, ParticleSize, ParticleSize);
            sb.Draw(pixel, rect, Color.Gold * alpha);
        }
    }
}
