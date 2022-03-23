namespace NoFS.DayLight.Sovereign {
   public class MeshHelper {
      public MeshHelper() {
         manager = SvrnMaster.inst.getMeshManagerInstance();
      }

      public readonly MeshManager manager;
   }
}