using FFXIVClientStructs.Havok.Animation.Rig;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Game.Posing.Skeletons;



public unsafe class PartialSkeleton(Skeleton skeleton, int id)
{
    public int Id { get; } = id;

    public Skeleton Skeleton { get; } = skeleton;

    public List<nint> Poses { get; set; } = [];

    private readonly Dictionary<int, Bone> _bones = [];

    public Bone? ParentBone { get; set; }

    public Bone RootBone { get; set; } = null!;

    public IReadOnlyDictionary<int, Bone> Bones => _bones;

    public Bone GetOrCreateBone(int index)
    {
        if(_bones.TryGetValue(index, out var bone))
            return bone;

        return _bones[index] = new Bone(index, Skeleton, this);
    }

    public Bone? GetBone(int index)
    {
        if(_bones.TryGetValue(index, out var bone))
            return bone;

        return null;
    }

    public Bone? GetBone(string name)
    {
        foreach(var bone in _bones.Values)
        {
            if(bone.Name == name)
                return bone;
        }

        return null;
    }

    public hkaPose* GetBestPose()
    {
        var best = Poses.FirstOrDefault(0);
        if(best == 0)
            return null;

        return (hkaPose*)best;
    }
}

