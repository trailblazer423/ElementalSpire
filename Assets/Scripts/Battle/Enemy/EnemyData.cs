using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "史莱姆";
    public int maxHP = 15;
    public int baseAttack = 6;
    public int baseDefend = 5;
    public EnemyType enemyType = EnemyType.Normal;
    public float attackChance = 0.6f;
}

public enum EnemyType
{
    Normal,
    Elite,
    Boss
}