using System;

namespace BurritoRun
{
    // Unity mouse coordinates start at bottom-left; IMGUI panels start at top-left.
    // Check the pointer before IMGUI receives the click so PlayerControl cannot
    // turn a quick-test button press into a world punch on the same frame.
    internal static class TestPanelInput
    {
        internal static bool BlocksWorldClick(float mouseX, float mouseYFromBottom,
            float screenWidth, float screenHeight, bool mouseHeldOrChanged)
        {
            if (!mouseHeldOrChanged) return false;
            float panelX = screenWidth - 275f;
            float panelHeight = Math.Min(720f, Math.Max(200f, screenHeight - 30f));
            float mouseYFromTop = screenHeight - mouseYFromBottom;
            return mouseX >= panelX && mouseX <= panelX + 260f &&
                mouseYFromTop >= 15f && mouseYFromTop <= 15f + panelHeight;
        }
    }
}
