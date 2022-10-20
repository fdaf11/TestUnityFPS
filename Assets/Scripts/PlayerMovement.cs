using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Animator anim;
    public CharacterController characterController;
    public float speed = 10f;  // 移动速度
    public Vector3 moveDirection; // 设定移动方向
    
    public float walkSpeed = 10f;  // 行走速度
    public float runSpeed = 15f;   // 奔跑速度
    public bool isRun;
    public bool isWalk;
    
    public float jumpForce=3f; // 跳跃力度
    public Vector3 velocity; // 冲量变化（力度）
    public bool isJump;
    
    public Transform groundCheck;
    public float gravity = -9f;
    
    private float groundDistance = 0.1f;  //  与地面的距离
    public LayerMask groundMesh;
    public bool isGround;
        
    [SerializeField]private float slopeForce = 6f;
    [SerializeField]private float slopeForceRayLength = 2f;
    
    
    [Header("键位设置")]
    [SerializeField][Tooltip("奔跑按键")]public KeyCode runInputName;  // 奔跑键位
    [SerializeField][Tooltip("跳跃按键")]public string jumpInputName = "Jump";  // 奔跑键位
    
    [Header("声音设置")]
    [SerializeField] private AudioSource audioSource;
    public AudioClip walkingSound;
    public AudioClip runingSound;
    
    
    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        runInputName = KeyCode.LeftShift;
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckGround();
        Move();
    }
    
    
    // 移动 
    public void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v= Input.GetAxis("Vertical");
        
        isRun = Input.GetKey(runInputName);
        isWalk = Mathf.Abs(h) > 0 || Mathf.Abs(v) > 0;
        speed = isRun ? runSpeed : walkSpeed;

        moveDirection = (transform.right * h + transform.forward * v).normalized;
        characterController.Move(moveDirection * speed * Time.deltaTime);
        if (isGround == false)
            velocity.y += gravity * Time.deltaTime; 
        
        characterController.Move(velocity * Time.deltaTime);
        Jump();
        
        if (OnSlope())  
        {
            characterController.Move(Vector3.down * characterController.height / 2 * slopeForce * Time.deltaTime);
        }

        PlayFootStepSound();
    }

    public void PlayFootStepSound()
    {
        if (isGround && moveDirection.sqrMagnitude > 0.9f)
        {
            audioSource.clip = isRun ? runingSound : walkingSound;
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
    }
    
    public void Jump()
    {
        isJump = Input.GetButtonDown(jumpInputName);
        if (isJump && isGround)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
    }

    void CheckGround()
    {
        isGround = Physics.CheckSphere(groundCheck.position, groundDistance, groundMesh);
        if (isGround && velocity.y <= 0)
            velocity.y = -2f;
    }

    public bool OnSlope()
    {
        if (isJump)
            return false;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit,
                characterController.height / 2 * slopeForceRayLength))
        {
            if (hit.normal != Vector3.up)
                return true;
        }

        return false;
    }
}
