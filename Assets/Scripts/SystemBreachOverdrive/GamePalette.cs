using System;
using UnityEngine;

namespace SystemBreachOverdrive
{
    public enum PaletteMode
    {
        Default = 0,
        ColorblindFriendly = 1
    }

    public readonly struct PaletteDefinition
    {
        public readonly Color Signal;
        public readonly Color Error;
        public readonly Color Required;
        public readonly Color Overloaded;

        public PaletteDefinition(Color signal, Color error, Color required, Color overloaded)
        {
            Signal = signal;
            Error = error;
            Required = required;
            Overloaded = overloaded;
        }
    }

    public static class GamePalette
    {
        private static readonly PaletteDefinition DefaultPalette = new PaletteDefinition(
            new Color(0.36f, 0.96f, 1.00f),
            new Color(1f, 0.15f, 0.35f),
            new Color(0.95f, 0.88f, 0.20f),
            new Color(0.96f, 0.56f, 0.14f));

        private static readonly PaletteDefinition ColorblindPalette = new PaletteDefinition(
            new Color(0.25f, 0.70f, 1.00f),
            new Color(1.00f, 0.55f, 0.10f),
            new Color(0.72f, 0.62f, 1.00f),
            new Color(0.15f, 0.85f, 0.65f));

        public static event Action OnPaletteChanged;

        public static PaletteMode CurrentMode { get; private set; } = PaletteMode.Default;

        public static PaletteDefinition Current
        {
            get
            {
                return CurrentMode == PaletteMode.ColorblindFriendly ? ColorblindPalette : DefaultPalette;
            }
        }

        public static void SetMode(PaletteMode mode)
        {
            if (CurrentMode == mode)
            {
                return;
            }

            CurrentMode = mode;
            OnPaletteChanged?.Invoke();
        }
    }
}
