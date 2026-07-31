using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Gacha;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Liste personnages d'une bannière : Hub featured/pool, sinon dérive legacy.
    /// Source unique pour compteur N et showcase 6.c.
    /// </summary>
    public static class BannerRoster
    {
        public const int MaxFeaturedPages = 6;

        /// <summary>
        /// Résout featured (ordre) + pool restant (hors featured).
        /// Si listes Hub vides → legacy rate-up + pools SR/SSR/LR.
        /// </summary>
        public static void Resolve(
            BannerData banner,
            List<CharacterData> featuredOut,
            List<CharacterData> poolOut)
        {
            featuredOut.Clear();
            poolOut.Clear();
            if (banner == null)
                return;

            bool hubHasFeatured = banner.FeaturedCharacters != null
                                  && banner.FeaturedCharacters.Count > 0;
            bool hubHasPool = banner.PoolCharacters != null
                              && banner.PoolCharacters.Count > 0;

            if (hubHasFeatured || hubHasPool)
            {
                if (hubHasFeatured)
                {
                    for (int i = 0; i < banner.FeaturedCharacters.Count; i++)
                    {
                        CharacterData c = banner.FeaturedCharacters[i];
                        if (c != null && !ContainsId(featuredOut, c.Id))
                            featuredOut.Add(c);
                    }
                }

                if (hubHasPool)
                {
                    for (int i = 0; i < banner.PoolCharacters.Count; i++)
                    {
                        CharacterData c = banner.PoolCharacters[i];
                        if (c == null || ContainsId(featuredOut, c.Id) || ContainsId(poolOut, c.Id))
                            continue;
                        poolOut.Add(c);
                    }
                }
            }
            else
            {
                // Legacy : rate-up en vedette, pools par rarete en pool.
                if (banner.RateUpSSR != null)
                    featuredOut.Add(banner.RateUpSSR);

                List<CharacterData> lr = banner.RateUpLR;
                if (lr != null)
                {
                    for (int i = 0; i < lr.Count; i++)
                    {
                        CharacterData c = lr[i];
                        if (c != null && !ContainsId(featuredOut, c.Id))
                            featuredOut.Add(c);
                    }
                }

                AppendPoolUnique(banner.SSRPool, featuredOut, poolOut);
                AppendPoolUnique(banner.LRPool, featuredOut, poolOut);
                AppendPoolUnique(banner.SRPool, featuredOut, poolOut);
            }
        }

        /// <summary> Total personnages (featured + pool résolus). </summary>
        public static int TotalCount(BannerData banner)
        {
            var featured = new List<CharacterData>(8);
            var pool = new List<CharacterData>(32);
            Resolve(banner, featured, pool);
            return featured.Count + pool.Count;
        }

        /// <summary>
        /// Split étage 1 (max 6) / étage 2 (surplus featured + pool).
        /// </summary>
        public static void SplitForShowcase(
            BannerData banner,
            List<CharacterData> etage1Out,
            List<CharacterData> etage2Out)
        {
            etage1Out.Clear();
            etage2Out.Clear();
            var featured = new List<CharacterData>(8);
            var pool = new List<CharacterData>(32);
            Resolve(banner, featured, pool);

            for (int i = 0; i < featured.Count; i++)
            {
                if (etage1Out.Count < MaxFeaturedPages)
                    etage1Out.Add(featured[i]);
                else
                    etage2Out.Add(featured[i]);
            }

            for (int i = 0; i < pool.Count; i++)
                etage2Out.Add(pool[i]);
        }

        private static void AppendPoolUnique(
            List<CharacterData> source,
            List<CharacterData> featured,
            List<CharacterData> pool)
        {
            if (source == null)
                return;
            for (int i = 0; i < source.Count; i++)
            {
                CharacterData c = source[i];
                if (c == null || ContainsId(featured, c.Id) || ContainsId(pool, c.Id))
                    continue;
                pool.Add(c);
            }
        }

        private static bool ContainsId(List<CharacterData> list, string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Id == id)
                    return true;
            }

            return false;
        }
    }
}
