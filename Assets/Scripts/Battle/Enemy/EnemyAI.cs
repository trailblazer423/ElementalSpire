using UnityEngine;

[System.Serializable]
public class EnemyAI : MonoBehaviour
{
    public int attackDamage = 6;
    public int defendAmount = 5;

    private enemyHP _enemyHP;
    private enemyBlock _enemyBlock;
    private EnemyState _enemyState;
    private playerHP _playerHP;

    void Start()
    {
        _enemyHP = GetComponent<enemyHP>();
        _enemyBlock = GetComponent<enemyBlock>();
        _enemyState = GetComponent<EnemyState>();

        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
            _playerHP = playerObject.GetComponent<playerHP>();
    }

    public void ExecuteTurn()
    {
        int action = Random.Range(0, 2);

        if (action == 0)
        {
            if (_playerHP == null) return;

            int finalDamage = attackDamage;
            if (_enemyState != null && _enemyState.TryConsumeDeepPoison())
            {
                finalDamage = Mathf.Max(1, Mathf.FloorToInt(finalDamage * 0.75f));
                Debug.Log($"【敌人行动】深度中毒生效，本次攻击降低为 {finalDamage}");
            }

            _playerHP.TakeDamage(finalDamage);
            Debug.Log($"【敌人行动】攻击！造成 {finalDamage} 点伤害");
        }
        else
        {
            _enemyBlock?.AddBlock(defendAmount);
            Debug.Log($"【敌人行动】防御！获得 {defendAmount} 点护盾");
        }
    }
}

