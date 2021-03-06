
using System.Collections.Generic;
using UnityEngine;


public class SkillBindings
{
    // Declare static, variables
    public static List<string> bindableButtons = new List<string>() { "_0", "_1", "1", "2", "3", "4", "f", "f" };

    private Dictionary<string, Skill> bindedButtons = new Dictionary<string, Skill>();


    public bool RequestBinding(Skill skill, string[] buttons, bool force = false)
    {
        // Try bind to each button
        foreach (string button in buttons)
        {
            if (RequestBinding(skill, button, force)) return true;
        }
        return false;
    }

    public bool RequestBinding(Skill skill, string button, bool force=false)
    {
        // Ensure is bindable and has not already been binded
        if (!bindableButtons.Contains(button)) return false;
        if (bindedButtons.ContainsKey(button))
        {
            if (force) Unbind(button);
            else return false;
        }
        bindedButtons.Add(button, skill);
        skill.Bind(this);
        return true;
    }

    public void Unbind(string button)
    {
        // Unbind button if bound
        if (bindedButtons.ContainsKey(button))
        {
            bindedButtons[button].Unbind();
            bindedButtons.Remove(button);
        }
    }

    public void Unbind(Skill skill)
    {
        // Find button for the skill
        string button = null;
        foreach (KeyValuePair<string, Skill> entry in bindedButtons)
        {
            if (entry.Value == skill) button = entry.Key;
        }

        // Unbind skill if bound
        if (button != null)
        {
            skill.Unbind();
            bindedButtons.Remove(button);
        }
    }


    public void UpdateSkills()
    {
        // Update all binded skills
        foreach (KeyValuePair<string, Skill> entry in bindedButtons) entry.Value.Update();
    }


    public void Use(string button)
    {
        // Call binded action for given button
        if (!bindedButtons.ContainsKey(button)) return;
        bindedButtons[button].Use();
    }


    public void Clear() => bindedButtons.Clear();
}


public abstract class Skill
{
    // Declare variables
    public bool isBinded { get; private set; } = false;
    public SkillBindings bindings { get; private set; }

    public bool isActive { get; protected set; } = false;
    public bool isCooldown { get; protected set; } = false;
    public float cooldownTimer { get; protected set; } = 0.0f;
    public float cooldownTimerMax { get; protected set; } = 1.0f;


    public Skill(float cooldownTimerMax_) { cooldownTimerMax = cooldownTimerMax_; }


    public virtual void Bind(SkillBindings bindings) => isBinded = true;

    public virtual void Unbind() => isBinded = false;


    public virtual void Update()
    {
        // Update cooldown timer
        if (isCooldown)
        {
            cooldownTimer = Mathf.Max(0.0f, cooldownTimer - Time.deltaTime);
            if (cooldownTimer == 0.0f) isCooldown = false;
        }
    }
    

    public virtual void Use() { }


    protected virtual bool GetUsable() => !isActive && !isCooldown;

    protected virtual void SetActive(bool isActive_)
    {
        // Update active, cooldown timer
        isActive = isActive_;
        if (!isActive && cooldownTimerMax != 0.0f)
        {
            isCooldown = true;
            cooldownTimer = cooldownTimerMax;
        }
    }
}
