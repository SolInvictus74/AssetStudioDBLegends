RazTools DBL Unity 6 Parser V0.3

Focus: SkinnedMeshRenderer correction.

V0.2 incorrectly inserted m_MaskInteraction before m_Quality. That field is not part of the SkinnedMeshRenderer payload at that location. This shifted the reader by four bytes and caused SkinnedMeshRenderer parsing failures.

V0.3 removes that read and preserves the established serialized layout:
  m_Quality (Int32)
  m_UpdateWhenOffscreen (bool)
  m_SkinnedMotionVectors / legacy m_SkinNormals slot (bool)
  align
  m_Mesh ...

The Unity 6 common Renderer additions from V0.2 remain in Renderer.cs.
Texture2D is intentionally NOT changed in this build so SkinnedMeshRenderer can be tested independently.

TEST:
- Build Release | x64
- Disable Renderer = OFF
- Load the new DB Legends Unity 6000.3.14f1 CAB
- Check whether SkinnedMeshRenderer errors disappear
- Test Go to Scene Hierarchy
- Texture2D errors are expected to remain for now
