using UnityEngine;

public class WaterSprinklerPS : MonoBehaviour
{
    public ParticleSystem spray;
    public float rateOn = 600f, rateOff = 0f;

    public void SetState(bool on)
    {
        if (!spray) return;
        var em = spray.emission;
        em.rateOverTime = on ? rateOn : rateOff;
        if (on && !spray.isPlaying) spray.Play();
        if (!on && spray.isPlaying) spray.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
