using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Cinemachine;

public class PlayerState : MonoBehaviour
{
    public TurnBasedPlayer player;
    private BattleStateMachine BSM;

    public enum TurnState
    {
        WAITING,
        ACTION,
        DEAD
    }
    public TurnState currentState;

    public GameObject selector;
    public GameObject enemyToAttack;
    private bool actionStarted = false;
    private Vector3 startPosition;
    private float animSpeed = 0.3f;
    public List<PlayerMove> attackMoves = new List<PlayerMove>();
    public float focusExtraEnergy = 1f;
    public float focusDamageIncrease = 0.15f;
    public float guardDamageReduction = 0.5f;
    private bool isGuarding = false;
    private bool isFocusing = false;

    // this gets set before attack animation plays
    private float pendingDamageMultiplier = 1f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPosition = transform.position;
        selector.SetActive(false);
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        currentState = TurnState.WAITING;

        // Initialize live stats from base values
        player.currentHP = player.baseHP;
        player.currentATK = player.baseATK;
        player.currentNRG = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == TurnState.ACTION)
        {
            StartCoroutine(TimeForAction());
        }
    }

    public void OnTurnStart()
    {
        GainEnergy(1f);
        isGuarding = false;
        isFocusing = false;
    }

    public void GainEnergy(float amount)
    {
        player.currentNRG = Mathf.Min(player.currentNRG + amount, player.baseNRG);
    }

    public bool HasEnoughEnergy(float cost)
    {
        return player.currentNRG >= cost;
    }

    public void SpendEnergy(float amount)
    {
        player.currentNRG = Mathf.Max(0f, player.currentNRG -  amount);
    }

    public void SetPendingDamageMultiplier(float multiplier)
    {
        pendingDamageMultiplier = multiplier;
    }

    public void DoGuard()
    {
        isGuarding = true;
        BSM.onActionComplete();
    }

    public void DoFocus()
    {
        isFocusing = true;
        GainEnergy(focusExtraEnergy);
        BSM.onActionComplete();
    }

    private IEnumerator TimeForAction()
    {
        if (actionStarted)
        {
            yield break;
        }

        actionStarted = true;

        //simple slide to player to animate attacking
        Vector3 enemyPosition = new Vector3(enemyToAttack.transform.position.x - 1.5f, enemyToAttack.transform.position.y, enemyToAttack.transform.position.z);
        while (MoveTowardsEnemy(enemyPosition)) { yield return null;}


        // wait 
        yield return new WaitForSeconds(0.5f);

        // do dmg
        EnemyState target = enemyToAttack.GetComponent<EnemyState>();
        if (target != null)
        {
            target.TakeDamage(player.currentATK);
        }

        // slide back
        Vector3 firstPosition = startPosition;
        while (MoveTowardsStart(firstPosition)) { yield return null; }

        actionStarted = false;
        currentState = TurnState.WAITING;

        BSM.onActionComplete();
    }

    public void TakeDamage(float amount)
    {
        float finalDamage = amount;

        if (isGuarding)
        {
            finalDamage *= (1f - guardDamageReduction);
        }
        if (isFocusing)
        {
            finalDamage *= (1f + focusDamageIncrease);
        }

        player.currentHP -= finalDamage;
        Debug.Log(player.name + " took " + finalDamage + " damage. HP now: " + player.currentHP);

        if (player.currentHP <= 0)
        {
            player.currentHP = 0;
            currentState = TurnState.DEAD;
            Debug.Log(player.name + " has died.");
            BSM.PlayerInBattle.Remove(this.gameObject);
            BSM.PlayersToManage.Remove(this.gameObject);
            gameObject.SetActive(false);
        }
    }

    private bool MoveTowardsEnemy(Vector3 target)
    {
        return target != (transform.position = Vector3.MoveTowards(transform.position, target, animSpeed));
    }

    private bool MoveTowardsStart(Vector3 target)
    {
        return target != (transform.position = Vector3.MoveTowards(transform.position, target, animSpeed));
    }
}
