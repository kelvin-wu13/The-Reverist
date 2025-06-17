using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM Settings")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private bool loopBGM = true;

    [Header("Player SFX")]
    [SerializeField] private AudioClip playerSpawnSFX;
    [SerializeField] private AudioClip playerDeathSFX;

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
    // Enemy Audio
    public void PlayEnemySpawnSFX() => PlaySFX(enemySpawnSFX);
    public void PlayEnemyShootSFX() => PlaySFX(enemyShootSFX);
    public void PlayEnemyDeathSFX() => PlaySFX(enemyDeathSFX);

    // Skill Audio
    public void PlayIonBoltSFX() => PlaySFX(IonBoltSFX);
    public void PlayPlasmaSurgeSFX() => PlaySFX(PlasmaSurgeSFX);
    public void PlayPulseFallSFX() => PlaySFX(PulseFallSFX);
    public void PlayQuickSlashSFX() => PlaySFX(QuickSlashSFX);
    public void PlaySwiftStrikeSFX() => PlaySFX(SwiftStrikeSFX);
    public void PlayKineticShoveSFX() => PlaySFX(KineticShoveSFX);
    public void PlayWilloWispSFX() => PlaySFX(WilloWispSFX);
    public void PlayMagneticPullSFX() => PlaySFX(MagneticPullSFX);
    public void PlayGridLockSFX() => PlaySFX(GridLockSFX);

    // Player Audio
    public void PlayBasicShootSFX() => PlaySFX(BasicShootSFX);
    public void PlayPlayerSpawnSFX() => PlaySFX(playerSpawnSFX);
    public void PlayPlayerDeathSFX() => PlaySFX(playerDeathSFX);


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
}
