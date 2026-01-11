using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public AudioClip mainClip;
        public bool loop;

        [Range(0.0f, 1f)]
        public float volume = 1f;

        public virtual void PlaySound(AudioSource source)
        {
            if (source == null || mainClip == null) return;

            source.volume = volume;

            if (loop)
            {
                if (source.clip != mainClip)
                    source.clip = mainClip;

                source.loop = true;
                if (!source.isPlaying)
                    source.Play();
                return;
            }

            source.loop = false;
            source.PlayOneShot(mainClip);
        }
    }

    [System.Serializable]
    public class SoundVariations : Sound
    {
        public AudioClip[] variations;
        public override void PlaySound(AudioSource source)
        {
            if (source == null) return;

            source.volume = volume;

            int variationCount = variations != null ? variations.Length : 0;
            if (variationCount <= 0)
            {
                base.PlaySound(source);
                return;
            }

            int random = Random.Range(-1, variationCount);

            AudioClip clip = null;
            if (random == -1)
            {
                clip = mainClip;
            }
            else
            {
                clip = variations[random];
            }

            if (clip != null) source.PlayOneShot(clip);
        }
    }

    [System.Serializable]
    public class MusicSound
    {
        public AudioClip[] variations;
        public float fadeSeconds = 1f;
        public bool alignToCurrent = true;
        public bool loop = true;

        public void Fade(int id)
        {
            if (variations == null || variations.Length == 0) return;
            if (id < 0 || id >= variations.Length) return;

            var clip = variations[id];
            if (clip == null) return;

            MusicDirector.GetInstance().FadeTo(clip, fadeSeconds, alignToCurrent, loop);
        }
    }

    public static void PlaySound(Sound sound, AudioSource source)
    {
        if (sound == null) return;

        sound.PlaySound(source);
    }
}
