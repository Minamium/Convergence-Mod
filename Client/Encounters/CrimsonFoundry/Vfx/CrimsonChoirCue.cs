namespace Convergence.Client.Encounters.CrimsonFoundry;

// Read-only accepted attack times. The visual rig never owns a hitbox or timer.
// Kept beside the Vfx foundation (FNA- and Terraria-free, same namespace as the rig) so the arm envelope
// (ScarletGestureMotion.ChoirArm) links into the domain tests and the offline previews unchanged.
internal readonly record struct CrimsonChoirCue(float Born, float Fire, float End, int Arm, bool Broad = false);
internal readonly record struct CrimsonChoirArm(float Shoulder, float Elbow, float Wrist, float Power, float Burst);
