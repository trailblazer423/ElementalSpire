using System.Collections;
using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 战斗特效管理器：加载卡牌专属 Prefab；未配置资源时生成元素粒子、投射物、爆炸和受击反馈。
/// </summary>
[DisallowMultipleComponent]
public sealed class VFXManager : MonoBehaviour
{
    private static VFXManager _instance;

    [Header("Fallback Particle Settings")]
    [SerializeField, Min(0.01f)] private float defaultParticleSize = 0.28f;
    [SerializeField, Min(0.1f)] private float defaultProjectileSpeed = 7f;
    [SerializeField, Min(0.05f)] private float defaultProjectileDuration = 0.32f;
    [SerializeField, Min(0f)] private float hitShakeDistance = 0.12f;
    [SerializeField, Min(0.01f)] private float hitShakeDuration = 0.24f;

    private Material _particleMaterial;

    public static VFXManager EnsureExists()
    {
        if (_instance != null)
            return _instance;

        _instance = FindObjectOfType<VFXManager>();
        if (_instance == null)
        {
            GameObject manager = new GameObject("VFXManager");
            _instance = manager.AddComponent<VFXManager>();
        }
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayAttackEffect(CardData cardData, ElementType chosenElement, Transform target)
    {
        if (target == null)
            return;

        ElementType element = ResolveElement(cardData, chosenElement);
        CardAnimationProfile profile = CardAnimationProfile.Resolve(cardData);
        GameObject prefab = cardData?.attackVfxPrefab;
        if (prefab == null && profile != null)
            prefab = profile.attackVfxPrefab;

        float particleSize = profile != null ? profile.particleSize : defaultParticleSize;
        float particleSpeed = profile != null ? profile.particleSpeed : defaultProjectileSpeed;
        Transform sourceTransform = GameObject.Find("Player")?.transform;
        Vector3 source = sourceTransform != null
            ? sourceTransform.position + Vector3.up * 0.6f
            : target.position + Vector3.left * 4f;

        StartCoroutine(ProjectileRoutine(prefab, source, target, element, particleSize, particleSpeed));
    }

    public void PlayMagicEffect(CardData cardData, ElementType chosenElement, Transform target)
    {
        if (target == null)
            return;
        ElementType element = ResolveElement(cardData, chosenElement);
        CardAnimationProfile profile = CardAnimationProfile.Resolve(cardData);
        GameObject prefab = cardData?.playVfxPrefab;
        if (prefab == null && profile != null)
            prefab = profile.playVfxPrefab;

        if (prefab != null)
            Destroy(Instantiate(prefab, target.position, Quaternion.identity), 3f);
        else
            CreateExplosion(target.position, GetElementColor(element),
                profile != null ? profile.particleSize : defaultParticleSize, false);
    }

    private IEnumerator ProjectileRoutine(GameObject prefab, Vector3 source, Transform target,
        ElementType element, float particleSize, float particleSpeed)
    {
        Color color = GetElementColor(element);
        GameObject projectile = prefab != null
            ? Instantiate(prefab, source, Quaternion.identity)
            : CreateProjectile(source, color, particleSize);

        Vector3 start = source;
        float distance = target != null ? Vector3.Distance(start, target.position) : 2f;
        float duration = particleSpeed > 0f
            ? Mathf.Clamp(distance / particleSpeed, 0.12f, 0.55f)
            : defaultProjectileDuration;
        float elapsed = 0f;

        while (elapsed < duration && projectile != null && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            Vector3 destination = target.position;
            Vector3 position = Vector3.Lerp(start, destination, eased);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.45f;
            projectile.transform.position = position;
            yield return null;
        }

        if (projectile != null)
            Destroy(projectile);
        if (target == null)
            yield break;

        CreateExplosion(target.position, color, particleSize, element == ElementType.Water);
        StartCoroutine(EnemyHitFeedback(target, element, color));
    }

    private GameObject CreateProjectile(Vector3 position, Color color, float particleSize)
    {
        GameObject root = new GameObject("CardProjectileVFX");
        root.transform.position = position;
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 0.3f;
        main.startLifetime = 0.24f;
        main.startSpeed = 0.35f;
        main.startSize = particleSize;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 90;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 65f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;
        ConfigureRenderer(root.GetComponent<ParticleSystemRenderer>());
        particles.Play();
        return root;
    }

    private void CreateExplosion(Vector3 position, Color color, float particleSize, bool iceCrystal)
    {
        GameObject root = new GameObject(iceCrystal ? "IceImpactVFX" : "MagicImpactVFX");
        root.transform.position = position;
        ParticleSystem particles = root.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = 0.45f;
        main.startLifetime = iceCrystal ? 0.65f : 0.42f;
        main.startSpeed = iceCrystal ? 2.8f : 3.8f;
        main.startSize = iceCrystal ? particleSize * 0.65f : particleSize;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(iceCrystal ? 34 : 28)) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = iceCrystal ? 0.2f : 0.32f;
        ConfigureRenderer(root.GetComponent<ParticleSystemRenderer>());
        particles.Play();
        Destroy(root, 1.4f);
    }

    private IEnumerator EnemyHitFeedback(Transform target, ElementType element, Color flashColor)
    {
        if (target == null)
            yield break;

        Vector3 originalPosition = target.localPosition;
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            renderers[i].color = Color.Lerp(renderers[i].color, flashColor, element == ElementType.Water ? 0.72f : 0.55f);
        }

        float elapsed = 0f;
        while (elapsed < hitShakeDuration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float strength = 1f - Mathf.Clamp01(elapsed / hitShakeDuration);
            target.localPosition = originalPosition + new Vector3(
                Mathf.Sin(elapsed * 82f) * hitShakeDistance * strength,
                Mathf.Cos(elapsed * 67f) * hitShakeDistance * 0.45f * strength,
                0f);
            yield return null;
        }

        if (target == null)
            yield break;
        target.localPosition = originalPosition;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = originalColors[i];
        }
    }

    private void ConfigureRenderer(ParticleSystemRenderer renderer)
    {
        if (renderer == null)
            return;
        if (_particleMaterial == null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
                _particleMaterial = new Material(shader);
        }
        if (_particleMaterial != null)
            renderer.material = _particleMaterial;
        renderer.sortingOrder = 500;
    }

    private static ElementType ResolveElement(CardData cardData, ElementType chosenElement)
    {
        if (chosenElement != ElementType.None && chosenElement != ElementType.Colorless)
            return chosenElement;
        return cardData != null ? cardData.elementType : ElementType.None;
    }

    public static Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: return new Color(1f, 0.24f, 0.04f, 0.95f);
            case ElementType.Poison: return new Color(0.35f, 1f, 0.16f, 0.95f);
            case ElementType.Water: return new Color(0.18f, 0.72f, 1f, 0.95f);
            default: return new Color(1f, 0.78f, 0.24f, 0.95f);
        }
    }
}
