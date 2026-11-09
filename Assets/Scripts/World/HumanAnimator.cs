using UnityEngine;

namespace Zulvyre
{
    public class HumanAnimator : MonoBehaviour
    {
        Animation anim;
        string looping = "idle";
        string oneShot;
        float oneShotTimer;
        bool held;

        public bool Animated => anim != null && anim["idle"] != null;

        public void Bind(Animation source)
        {
            anim = source;
            if (anim == null) return;
            anim.playAutomatically = false;
            if (anim["idle"] != null) anim.Play("idle");
        }

        public void SetLoop(string state)
        {
            if (held || state == looping || anim == null || anim[state] == null) return;
            looping = state;
            if (oneShot == null) anim.CrossFade(state, 0.18f);
        }

        public void Trigger(string state, float rate = 1f, bool hold = false)
        {
            if (held || anim == null || anim[state] == null) return;
            var clip = anim[state];
            clip.speed = rate;
            clip.enabled = true;
            oneShot = state;
            oneShotTimer = 0f;
            held = hold;
            anim.CrossFade(state, 0.07f);
        }

        public void ResetPose()
        {
            if (anim == null) return;
            held = false;
            oneShot = null;
            oneShotTimer = 0f;
            looping = "idle";
            anim.CrossFade("idle", 0.2f);
        }

        void Update()
        {
            if (anim == null || oneShot == null || held) return;
            var clip = anim[oneShot];
            oneShotTimer += Time.deltaTime * clip.speed;
            if (oneShotTimer < clip.length) return;
            oneShot = null;
            clip.speed = 1f;
            anim.CrossFade(looping, 0.2f);
        }
    }
}
