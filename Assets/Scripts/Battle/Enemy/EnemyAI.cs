using UnityEngine;

[System.Serializable]
public class EnemyAI : MonoBehaviour
{
    public int attackDamage = 6;
    public int defendAmount = 5;

    private enemyHP _enemyHP;
    private enemyBlock _enemyBlock;
    private playerHP _playerHP;

    void Start()
    {
        _enemyHP = GetComponent<enemyHP>();
        _enemyBlock = GetComponent<enemyBlock>();
        _playerHP = GameObject.Find("Player").GetComponent<playerHP>();
    }

    public void ExecuteTurn()
    {
        int action = Random.Range(0, 2);

        if (action == 0)
        {
            _playerHP.TakeDamage(attackDamage);
            Debug.Log($"【敌人行动】攻击！造成 {attackDamage} 点伤害");
        }
        else
        {
            _enemyBlock.AddBlock(defendAmount);
            Debug.Log($"【敌人行动】防御！获得 {defendAmount} 点护盾");
        }
    }
}