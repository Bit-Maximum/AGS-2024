using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyAttack : MonoBehaviour
{
    private AnimetionPatrolEnemy AnimationControl;

    private float LastSwingTime;
    private float LastHurtTime;
    private float LastAttackTime;

    [SerializeField] public float AttackSwingDurationTime;
    [SerializeField] public float HurtDurationTime;
    [SerializeField] public float AttackCooldownTime;

    public bool IsAggro = false;
    public bool IsAttack = false;
    public bool IsSwing = false;

    public float aggroRange;
    public float attackRange;
    public float chaseSpeed;

    public int attackDamage = 1;
    

    [SerializeField] public Transform player;
    [SerializeField] private Transform _frontAttackCheckPoint;
    [SerializeField] private Vector2 _frontAttackCheckSize = new Vector2(0.5f, 1f);

    [SerializeField] private LayerMask _playerLayer;


    public void Awake()
    {
        AnimationControl = GetComponent<AnimetionPatrolEnemy>();
    }

    private void Update()
    {
        LastSwingTime -= Time.deltaTime;
        LastHurtTime -= Time.deltaTime;
        LastAttackTime -= Time.deltaTime;
    }

    private void FixedUpdate()
    {

        if (Vector2.Distance(transform.position, player.position) <= aggroRange)
        {
            IsAggro = true;
        }
        else
        {
            IsAggro = false;
            IsSwing = false;
            IsAttack = false;
        }

        if (IsAggro)
        {
            if (Vector2.Distance(transform.position, player.position) > attackRange)
            {
                AnimationControl.SetLostTargetPrarametr(true);
                IsSwing = false;
                IsAttack = false;

                if (transform.position.y < player.position.y)
                {
                    // Set new Y-position to current Y-height to prevent enemy from flying towards the player
                    Vector3 targetPossition = new Vector3(player.position.x, transform.position.y, transform.position.z);
                    transform.position = Vector2.MoveTowards(transform.position, targetPossition, chaseSpeed * Time.fixedDeltaTime);
                }
                transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.fixedDeltaTime);
            }
            else
            {
                AnimationControl.SetLostTargetPrarametr(false);
                if (!IsSwing && IsAttack && LastSwingTime < 0)
                {
                    AnimationControl.TrigerAttackAnimation();
                    LastAttackTime = AttackCooldownTime;
                    IsAttack = false;
                } 
                else if (!IsAttack && IsSwing && LastAttackTime < 0)
                {
                    AnimationControl.TrigerAttackSwingAnimation();
                    LastSwingTime = AttackSwingDurationTime;
                    IsSwing = false;
                    IsAttack = true;
                }
                else if (!IsSwing && !IsAttack)
                {
                    IsSwing = true;
                }

            }
        }
    }

    public void Attack()
    {
        //checks if set box overlaps with Player
        Collider2D player = Physics2D.OverlapBox(_frontAttackCheckPoint.position, _frontAttackCheckSize, 0, _playerLayer);
        if (player) 
        {
            PlayerStatus playerStatus = player.GetComponent<PlayerStatus>();
            if (playerStatus)
            {
                playerStatus.TakeDamage(attackDamage);
            }
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (!player)
            return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_frontAttackCheckPoint.position, _frontAttackCheckSize);
    }
}