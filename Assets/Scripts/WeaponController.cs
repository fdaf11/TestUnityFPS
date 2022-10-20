using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponController : MonoBehaviour
{
    public PlayerMovement PM;
    public Transform shootPoint;
    public int bulletsMag = 31;		// 弹匣子弹数量
    public int range = 100;       // 武器射程
    public int bulletLeft = 300;		// 备弹
    public int currentBullets;			// 当前子弹数量
    private bool GunShootInput;
    public bool isAiming;
    
    public float fireRate = 0.1f;//射速
    private float fireTimer;//计时器
    private Camera mainCamera;
    
    public Transform casingSpawnPoint;//子弹壳抛出的位置
    public Transform casingPrefab;//子弹壳预制体
        
    [Header ("UI设置")]
    public Image CrossHairUl;
    public Text AmmoTextUl;
    public Text ShootModeTextUI;

    [Header ("键位设置")]
    [SerializeField][Tooltip("填装子弹按键")] private KeyCode reloadInputName;
    [SerializeField][Tooltip("查看枪械按键")] private KeyCode inspectInputName;
    [SerializeField][Tooltip("自动/半自动切换按键")] private KeyCode GunShootModeInputName;

    public ParticleSystem muzzleFlash;//枪口火焰特效
    public Light muzzleFlashLight;//枪口火焰灯光
    public GameObject hitParticle;     // 子弹击中粒子特效
    public GameObject bulletHole;   // 弹孔
    public bool isReload;
    
    /*使用枚举区分全自动和半自动类型标签*/
    public enum ShootMode { AutoRife,SemiGun} ;
    public ShootMode shootingMode;
    private bool GunShootInputMode;//根据全自动和半自动状态不同，射击的键位输入发生改变
    private string shootName;
    
    [Header ("声音")]
    private AudioSource audioSource;
    public AudioClip AK47SoundClip;/*枪射击音效片段*/
    
    public AudioClip reloadAmmoLeftClip;//换子弹音效1片段
    public AudioClip reloadOutOfAmmoleftClip;//换子弹音效1片段
    
    [Header("动画")]
    private Animator anim;
    
    
    
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        currentBullets = bulletsMag;
        reloadInputName = KeyCode.R;
        inspectInputName = KeyCode.I;
        GunShootModeInputName = KeyCode.X;
        anim = GetComponent<Animator>();
        mainCamera = Camera.main;
        shootingMode = ShootMode.AutoRife;
        shootName = "全自动";
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(GunShootModeInputName) && shootingMode != ShootMode.AutoRife)
        {
            shootName = "全自动";
            shootingMode = ShootMode.AutoRife;
            ShootModeTextUI.text = shootName;
        }
        else if (Input.GetKeyDown(GunShootModeInputName) && shootingMode != ShootMode.SemiGun)
        {
            shootName = "半自动";
            shootingMode = ShootMode.SemiGun;
            ShootModeTextUI.text = shootName;
        }

        switch (shootingMode)
        {
            case ShootMode.AutoRife:
                GunShootInput = Input.GetMouseButton(0);
                fireRate = 0.1f;
                break;
            case ShootMode.SemiGun:
                GunShootInput = Input.GetMouseButtonDown(0);
                fireRate = 0.2f;
                break;
        }

        if (GunShootInput && currentBullets > 0f)
        {
            GunFire();
        }
        else
        {
            muzzleFlashLight.enabled = false;
        }

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("reload_ammo_left") || info.IsName("reload_out_of_ammo"))
            isReload = true;
        else
            isReload = false;
        
        if (Input.GetKeyDown(reloadInputName) && currentBullets < bulletsMag && bulletLeft > 0)
        {
            Reload();
        }

        DoingAim();

        if (Input.GetKeyDown(inspectInputName))
        {
            anim.SetTrigger("Inspect");
        }

        if (fireTimer < fireRate)
        {
            fireTimer += Time.deltaTime;
        }
        
        anim.SetBool("Run", PM.isRun);
        anim.SetBool("Walk", PM.isWalk);
    }

    ///<summary>///瞄准的逻辑/ / / </summary>
    public void DoingAim()
    {
        if (Input.GetMouseButton(1) && !isReload && !PM.isRun)
        {
            //瞄准准星消失，视野靠前
            isAiming = true;
            anim.SetBool("Aim",true);
            CrossHairUl.gameObject.SetActive(false);
            mainCamera.fieldOfView = 25;//瞄准的时候摄像机视野变小
        }
        else
        {
            //非瞄准
            isAiming = false;
            anim.SetBool ("Aim", false);
            CrossHairUl.gameObject.SetActive(true) ;
            mainCamera.fieldOfView = 60;//瞄准的时候摄像机视野变大   
        }
        
    }

    public void GunFire()
    {
        if (fireTimer < fireRate || isReload || PM.isRun)
            return;

        RaycastHit hit;
        Vector3 shootDirection = shootPoint.forward;
        if (Physics.Raycast(shootPoint.position, shootDirection, out hit, range))
        {
            Debug.Log(hit.collider.gameObject.name + " Hit!");
            GameObject hitParticleEffect= Instantiate(hitParticle, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
            GameObject bullectHoleEffect= Instantiate(bulletHole, hit.point,Quaternion.FromToRotation(Vector3.up, hit.normal));
            Destroy(hitParticleEffect, 1f);
            Destroy(bullectHoleEffect, 3f);
        }

        if (!isAiming)
        {
            anim.CrossFadeInFixedTime("fire", 0.1f);
        }
        else
        {
            anim.CrossFadeInFixedTime("aim_fire", 0.1f);
        }
        
        Instantiate(casingPrefab, casingSpawnPoint.transform.position, casingSpawnPoint.transform.rotation);

        muzzleFlashLight.enabled = true;
        PlayerShootSound();
        muzzleFlash.Play();
        currentBullets--;
        currentBullets = Mathf.Max(currentBullets, 0);
        UpdateAmmoUI();
        
        fireTimer = 0f;
    }

    public void UpdateAmmoUI()
    {
        AmmoTextUl.text = currentBullets + " / " + bulletLeft;
        ShootModeTextUI.text = shootName;
    }

    public void Reload()
    {
        if (bulletLeft <= 0)
            return;

        DoReloadAnimation();
        int bulletToLoad = bulletsMag - currentBullets;

        int bulletToReduce = (bulletLeft >= bulletToLoad) ? bulletToLoad : bulletLeft;
        bulletLeft -= bulletToReduce;
        currentBullets += bulletToReduce;
        UpdateAmmoUI();
    }

    /// <summary>///播放装弹动画/  / </summary>
    public void DoReloadAnimation()
    {
        if (currentBullets > 0)
        {
            anim.Play ("reload_ammo_left",0, 0);
            audioSource.clip = reloadAmmoLeftClip;
            audioSource.Play();
        }

        if (currentBullets == 0)
        {
            //播放动画2
            anim.Play ("reload_out_of_ammo",0,0);
            audioSource.clip = reloadOutOfAmmoleftClip;
            audioSource.Play();
        }
    }

    public void PlayerShootSound()
    {
        audioSource.clip = AK47SoundClip;
        audioSource.Play();
    }
}
