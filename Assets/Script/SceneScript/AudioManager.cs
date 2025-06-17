using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM Settings")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private bool loopBGM = true;

    [Header("Skill SFX")]
    public AudioClip BasicShootSFX;
    public AudioClip IonBoltSFX;
    public AudioClip PlasmaSurgeSFX;
    public AudioClip PulseFallSFX;
    public AudioClip QuickSlashSFX;
    public AudioClip SwiftStrikeSFX;
    public AudioClip KineticShoveSFX;
    public AudioClip WilloWispSFX;
    public AudioClip MagneticPullSFX;
    public AudioClip GridLockSFX;

    [Header("Enemy SFX")]
    public AudioClip enemySpawnSFX;
    public AudioClip enemyShootSFX;
    public AudioClip enemyDeathSFX;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Setup audio sources
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = loopBGM;
            bgmSource.playOnAwake = false;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void PlayEnemySpawnSFX() => PlaySFX(enemySpawnSFX);
    public void PlayEnemyShootSFX() => PlaySFX(enemyShootSFX);
    public void PlayEnemyDeathSFX() => PlaySFX(enemyDeathSFX);

    private void Start()
    {
        PlayBGM();
    }

    public void PlayBGM()
    {
        if (backgroundMusic != null)
        {
            bgmSource.clip = backgroundMusic;
            bgmSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayGridLockSFX()
    {
        PlaySFX(GridLockSFX);
    }
}
