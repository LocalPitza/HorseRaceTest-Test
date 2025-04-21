using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Horse : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveForce = 5f;
    public float torqueForce = 2f;
    public float targetSpeed = 8f;
    public float changeDirectionInterval = 2f;
    public float speedMaintenanceForce = 0.5f;
    
    [Header("Audio Settings")]
    public AudioClip wallHitSound;
    public AudioClip horseHitSound;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;
    
    [Header("Components")]
    public Rigidbody2D rb;
    public SpriteRenderer sr;
    public AudioSource audioSource;
    
    [HideInInspector] public bool canMove = false;
    public bool hasWon = false;
    private float directionTimer = 0f;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        rb.gravityScale = 0f;
        rb.drag = 0.05f; // Very low drag for consistent speed
        rb.angularDrag = 0.05f;
    }
    
    public void StartMoving()
    {
        canMove = true;
        ApplyRandomForce();
    }
    
    void FixedUpdate()
    {
        if (!hasWon && canMove)
        {
            directionTimer += Time.fixedDeltaTime;
            
            // Change direction periodically
            if (directionTimer >= changeDirectionInterval)
            {
                ApplyRandomForce();
                directionTimer = 0f;
            }
            
            // Maintain constant speed
            MaintainSpeed();
        }
    }
    
    private void ApplyRandomForce()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        rb.AddForce(randomDirection * moveForce, ForceMode2D.Impulse);
        rb.AddTorque(Random.Range(-1f, 1f) * torqueForce, ForceMode2D.Impulse);
    }
    
    private void MaintainSpeed()
    {
        if (rb.velocity.magnitude < targetSpeed)
        {
            Vector2 speedMaintenance = rb.velocity.normalized * speedMaintenanceForce;
            rb.AddForce(speedMaintenance, ForceMode2D.Force);
        }
        else if (rb.velocity.magnitude > targetSpeed * 1.1f)
        {
            rb.velocity = rb.velocity.normalized * targetSpeed;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canMove) return;
        
        // Play sound when hitting walls
        if (collision.gameObject.CompareTag("Wall") && wallHitSound != null && audioSource != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.PlayOneShot(wallHitSound);
            }
        }
        // Handle horse-to-horse collisions
        else if (collision.gameObject.CompareTag("Horse"))
        {
            Horse otherHorse = collision.gameObject.GetComponent<Horse>();
            
            // Play collision sound
            if (horseHitSound != null && audioSource != null && !audioSource.isPlaying)
            {
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.PlayOneShot(horseHitSound);
            }

            // Calculate repulsion force
            Vector2 awayDirection = (transform.position - collision.transform.position).normalized;
            float collisionForce = 1.5f; // Adjust this value for stronger/weaker repulsion
            
            // Apply forces to both horses
            rb.AddForce(awayDirection * moveForce * collisionForce, ForceMode2D.Impulse);
            
            if (otherHorse != null && otherHorse.rb != null)
            {
                otherHorse.rb.AddForce(-awayDirection * moveForce * collisionForce, ForceMode2D.Impulse);
            }
        }
        
        // Bounce off surfaces (walls)
        if (collision.gameObject.CompareTag("Wall") && rb.velocity.magnitude > 1f)
        {
            Vector2 reflectDirection = Vector2.Reflect(rb.velocity.normalized, collision.contacts[0].normal);
            rb.velocity = reflectDirection * targetSpeed;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Finish") && !hasWon && canMove)
        {
            hasWon = true;
            GameManager.Instance.HorseWon(this);
        }
    }
}
