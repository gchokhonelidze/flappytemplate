using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FlappyTemplate
{
    // Sprites from urls, for the pictures the server sends as links rather than as assets: a currency icon, a
    // game thumbnail, a player's avatar.
    //
    //     UiRemoteImage.Load(transaction.GameImg, sprite => image.sprite = sprite);
    //
    // Deliberately not a MonoBehaviour and deliberately not a coroutine. A download hung on the object that
    // asked for it dies when that object is closed or destroyed - which for a dialog is the common case, since
    // a picture arriving is often the last thing that happens before the player shuts it. UnityWebRequest's
    // own completed event needs nothing to run on, so a download outlives whatever started it and the answer
    // is in the cache by the time the dialog is opened again.
    //
    // Nothing is ever evicted. These are a handful of small images per session, and a dialog opened twenty
    // times should fetch them once.
    internal static class UiRemoteImage
    {
        private static readonly Dictionary<string, Sprite> Ready = new Dictionary<string, Sprite>();

        // Callbacks waiting on a url already in flight. Two coins of the same currency on one dialog is the
        // ordinary case, and it should be one download.
        private static readonly Dictionary<string, List<Action<Sprite>>> Waiting =
            new Dictionary<string, List<Action<Sprite>>>();

        // A url that answered with something that is not a picture, or did not answer at all. Remembered so
        // it is not asked again every time a window opens, and kept apart from Ready so that a cache entry
        // which has gone null can be told from one that was always going to be.
        private static readonly HashSet<string> Failed = new HashSet<string>();

        /// <summary>The sprite for a url if it has already been fetched. False while it is on its way, and for
        /// one that failed.</summary>
        public static bool TryGet(string url, out Sprite sprite)
        {
            sprite = null;

            if (string.IsNullOrEmpty(url) || !Ready.TryGetValue(url, out var found) || found == null)
                return false;

            sprite = found;
            return true;
        }

        /// <summary>Fetches a url and hands the sprite over, or null if it could not be had. A url already
        /// fetched answers immediately and in the same call.</summary>
        // The callback fires on the main thread either way, so it is free to touch the scene. It is also free
        // to belong to something that has since been destroyed, which is the caller's to check - a lambda
        // holding a reference to a dead component is not something this can see.
        public static void Load(string url, Action<Sprite> done)
        {
            if (string.IsNullOrEmpty(url) || done == null)
                return;

            if (Failed.Contains(url))
            {
                done(null);
                return;
            }

            if (Ready.TryGetValue(url, out var ready))
            {
                // A sprite nothing was holding on to can be collected by the scene load that calls
                // UnloadUnusedAssets, which leaves a destroyed reference behind in here. Fetching it again is
                // the only way back, and cheap - it will be in the http cache.
                if (ready != null)
                {
                    done(ready);
                    return;
                }

                Ready.Remove(url);
            }

            if (Waiting.TryGetValue(url, out var waiting))
            {
                waiting.Add(done);
                return;
            }

            Waiting[url] = new List<Action<Sprite>> { done };
            Fetch(url);
        }

        /// <summary>Forgets everything fetched, so the next ask goes out again. For a game that has changed
        /// account or currency and does not want the last one's coin.</summary>
        public static void Clear()
        {
            Ready.Clear();
            Failed.Clear();
        }

        private static void Fetch(string url)
        {
            var request = UnityWebRequestTexture.GetTexture(url);

            // No yield and nothing to yield on: the operation carries its own completion, which is what lets
            // this run without a MonoBehaviour to live on.
            request.SendWebRequest().completed += _ => Finish(url, request);
        }

        private static void Finish(string url, UnityWebRequest request)
        {
            Sprite made = null;

            if (request.result == UnityWebRequest.Result.Success)
                made = Build(DownloadHandlerTexture.GetContent(request));
            else
                Debug.LogWarning($"[UiRemoteImage] {url}: {request.error}");

            request.Dispose();

            if (made != null)
                Ready[url] = made;
            else
                Failed.Add(url);

            if (!Waiting.TryGetValue(url, out var waiting))
                return;

            Waiting.Remove(url);

            foreach (var callback in waiting)
            {
                // One caller throwing is not the others' problem - a dialog that has gone away half way
                // through the list should not keep the rest of them from being told.
                try
                {
                    callback?.Invoke(made);
                }
                catch (Exception error)
                {
                    Debug.LogException(error);
                }
            }
        }

        private static Sprite Build(Texture2D texture)
        {
            if (texture == null)
                return null;

            // Neither the texture nor the sprite belongs to a scene, and HideAndDontSave is what keeps the
            // UnloadUnusedAssets inside a scene load from collecting the ones no Image happens to be showing
            // at that moment.
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect);

            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
