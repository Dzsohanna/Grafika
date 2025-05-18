namespace Szeminarium1_24_02_17_2
{
    internal class CubeArrangementModel
    {
        /// <summary>
        /// Gets or sets whether the animation should run or it should be frozen.
        /// </summary>
        public bool AnimationEnabled { get; set; } = false;

        internal void AdvanceTime(double deltaTime)
        {
            // Animation is completely disabled
            return;
        }
    }
}