using UnityEngine;
using Color = UnityEngine.Color;

public class EnemyIdentity : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public enum EnemyRole
    {
        Guard,
        Medic,
        Engineer
    }

    public enum EnemyStage
    {
        Stage1,
        Stage2,
        Stage3
    }

    [Header("Enemy Identity")]
    public EnemyRole role = EnemyRole.Guard;
    public EnemyStage stage = EnemyStage.Stage1;

    private Renderer[] _renderer;    // every mesh under this enemy (a model has several)

    void Start()
    {
        _renderer = GetComponentsInChildren<Renderer>();    // includes this object's own renderer, so the capsule still works
        SetColor();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetColor()
    {
        Color baseColor;
        switch (role)
        {
            case EnemyRole.Guard:
                baseColor = Color.cyan;
                break;
            case EnemyRole.Medic:
                baseColor = Color.green;
                break;
            case EnemyRole.Engineer:
                baseColor = Color.red;
                break;
            default:
                baseColor = Color.white;
                break;
        }

        if (stage == EnemyStage.Stage2)
        {
            baseColor = baseColor * 0.7f;
        }
        else if (stage == EnemyStage.Stage3)
        {
            baseColor = baseColor * 0.4f;
        }

        // tint every part of the model, not just one renderer
        foreach (Renderer r in _renderer)
        {
            r.material.color = baseColor;
        }
    }
}
