using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
public class Shoot : NetworkBehaviour
{
    [SerializeField] GameObject muzzle;
    [SerializeField] ShootAnimation shootAnim;
    [SerializeField] GameObject weaponHolder;
    [SerializeField] GameObject GunRotation;
    [SerializeField] Camera mCamera;
    [SerializeField] bool limitAmmunition = true;

    private Weapon weapon;
    private RaycastHit crosshairHitPoint;
    private Vector3 _pointDirection;
    private Quaternion _lookRotation;
    private Vector3 hitpos;
    private RaycastHit hit;
    private Ray ray;
    private bool updateCanvas = true;
    private int curAmmo = 1, totalAmmo = 1;

    public int CurAmmo { get => curAmmo; set => curAmmo = value; }
    public int TotalAmmo { get => totalAmmo; set => totalAmmo = value; }

    private void Start() {
        
        if (isLocalPlayer) {
            weapon = weaponHolder.GetComponent<Weapon>();
            shootAnim.OnSwitchWeapon(weapon.Firerate);
            curAmmo = weapon.CurrentAmmunition;
            totalAmmo = weapon.TotalAmmunition;
        }
    }

    private void Update() {
        if (isLocalPlayer) {
            if (updateCanvas) {
                curAmmo = weapon.CurrentAmmunition;
                totalAmmo = weapon.TotalAmmunition;
                updateCanvas = false;
            }
            if (Input.GetButtonDown("Fire")) {
                updateCanvas = true;
                CmdFireBullet();
            }
            if (Input.GetButtonDown("Reload")) {
                updateCanvas = true;
                CmdReloadWeapon();
            }
            
        }
    }

    [Command]
    private void CmdReloadWeapon() {
        if (weapon.AllowAction && limitAmmunition) {
            reloadWeapon(weapon);
        }
    }

   [Command]
    // This code will be executed on the Server.
    private void CmdFireBullet() {
        ray = new Ray(mCamera.transform.position, mCamera.transform.forward); // Raycast from Camera
        if(Physics.Raycast(ray, out crosshairHitPoint, 5000f)) { // Check if Raycast is beyond 5000
            hitpos = crosshairHitPoint.point; // If hitpoint is under 5000
        } else { 
            hitpos = mCamera.transform.position + mCamera.transform.forward * 5000; 
        }
        _pointDirection = hitpos - muzzle.transform.position;
        _lookRotation = Quaternion.LookRotation(_pointDirection);
        GunRotation.transform.rotation = Quaternion.RotateTowards(GunRotation.transform.rotation, _lookRotation, 1f); // Point weapon to raycast hitpoint from camera

        if (weapon.AllowAction) { // If not reloading etc.
            if (Physics.Raycast(muzzle.transform.position, muzzle.transform.forward, out hit) && weapon.CurrentAmmunition > 0) { // Raycast from Bullet Exit Point to camera raycast 
                shootAnimation(); // Start Shoot Animation
                Debug.DrawLine(muzzle.transform.position, hit.point);

                bulletHole(GameObject.CreatePrimitive(PrimitiveType.Sphere), hit); // Creates bullethole where raycast hits

                if (hit.transform.gameObject.GetComponent<Player>() != null) { // If hit object is a player
                    Debug.Log("-->HIT PLAYER: " + hit.transform.name);
                    hit.transform.gameObject.GetComponent<Player>().RemoveHealth(weapon.Damage); 
                }
            }
            if (limitAmmunition) {
                subtractAmmunition(weapon); // Subtract Ammunition 
            }
            StartCoroutine(fireRate());
        }        
    }

    void bulletHole(GameObject holeObject, RaycastHit hit)
    {
        holeObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        holeObject.transform.position = hit.point;
    }

    [Client]
    void shootAnimation() {
        shootAnim.recoil(0.1f);
    }


    IEnumerator fireRate() {
        weapon.AllowAction = false;
        yield return new WaitForSeconds(60f/weapon.Firerate); // Waits for firerate seconds
        weapon.AllowAction = true;
    }

    private bool subtractAmmunition(Weapon weapon) { // Subtracts Ammunition from weapon 
        if (weapon.CurrentAmmunition > 0) {
            weapon.CurrentAmmunition -= 1;
            return true;
        }
        return false;
    }

    private bool reloadWeapon(Weapon weapon) {  // Reloads Ammunition from weapon 
        if (weapon.AllowAction && weapon.TotalAmmunition > 0) {
            weapon.AllowAction = false;
            int dif = weapon.MagazinSize - weapon.CurrentAmmunition;
            if (weapon.TotalAmmunition >= dif) {
                weapon.CurrentAmmunition += dif;
                weapon.TotalAmmunition -= dif;
            }
            else {
                weapon.CurrentAmmunition += weapon.TotalAmmunition;
                weapon.TotalAmmunition = 0;
            }
            weapon.AllowAction = true;
            Debug.Log("Reloaded");
            return true;
        }
        return false;
    }


}
