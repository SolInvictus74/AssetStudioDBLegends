using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public abstract class Texture : NamedObject
    {
        protected Texture(ObjectReader reader) : base(reader)
        {
            // ============================================================
            // UNITY 6
            //
            // Verified against the real ClassID 28 TypeTree.
            //
            // Serialized immediately after m_Name:
            //   bool m_IsAlphaChannelOptional
            //   alignment to 4 bytes
            // ============================================================

            if (version[0] >= 6000)
            {
                var m_IsAlphaChannelOptional =
                    reader.ReadBoolean();

                reader.AlignStream();

                return;
            }

            // ============================================================
            // LEGACY
            // ============================================================

            if (version[0] > 2017 ||
                (version[0] == 2017 && version[1] >= 3))
            {
                var m_ForcedFallbackFormat =
                    reader.ReadInt32();

                var m_DownscaleFallback =
                    reader.ReadBoolean();

                if (version[0] > 2020 ||
                    (version[0] == 2020 && version[1] >= 2))
                {
                    var m_IsAlphaChannelOptional =
                        reader.ReadBoolean();
                }

                reader.AlignStream();
            }
        }
    }
}
