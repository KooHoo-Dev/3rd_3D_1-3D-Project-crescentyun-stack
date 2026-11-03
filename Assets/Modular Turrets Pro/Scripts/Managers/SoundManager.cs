// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager instance;
        public AudioSource soundObject;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        // Spawn sound at point and play
        public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
        {
            if (audioClip == null)
            {
                return;
            }

            AudioSource audioSource = Instantiate(soundObject, spawnTransform.position, Quaternion.identity);

            audioSource.clip = audioClip;
            audioSource.volume = volume;

            audioSource.Play();

            float clipLength = audioSource.clip.length;

            Destroy(audioSource.gameObject, clipLength);

        }

    }
}