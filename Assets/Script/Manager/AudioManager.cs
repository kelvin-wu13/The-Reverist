using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM Settings")]
    [SerializeField] private AudioClip mainMenuBGM;
    [SerializeField] private AudioClip gameplayBGM;
    [SerializeField] private bool loopBGM = true;


    [Header("UI SFX")]
    [SerializeField] private AudioClip buttonHoverSFX;
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip buttonSelectSFX;

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

    // UI Audio
    public void PlayButtonHoverSFX() => PlaySFX(buttonHoverSFX);
    public void PlayButtonClickSFX() => PlaySFX(buttonClickSFX);
    public void PlayButtonSelectSFX() => PlaySFX(buttonSelectSFX);

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "MainMenu") 
        {
            PlayBGM(mainMenuBGM);
        }
        else
        {
            PlayBGM(gameplayBGM);
        }
    }


    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return; // avoid restarting same clip

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }
}
