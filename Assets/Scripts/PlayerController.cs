﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Player;

// This is my player movement class. You schould create your own.
// Adjust ServerReceive, ServerManager and the Consolidator classes with your controller.
// TODO: Split into ServerController and PlayerController (too messy).

public class PlayerController : MonoBehaviour {
    [Header("Movement Preferences")]
    [SerializeField]
    //private float gravity = -9.81F;
    private float gravity = -19.62F;
    [SerializeField]
    private float moveSpeed = 5F;
    [SerializeField]
    //private float jumpSpeed = 5F;
    private float jumpSpeed = 9F;

    private bool[] _inputs;
    private float _yVelocity = 0;

    public Player player = null;
    private CharacterController _characterController = null;

    void Start() {
        player = GetComponent<Player>();
        if (player == null)
            Debug.LogWarning("PlayerController::Start(): has not found the player reference. Please attach script on same object as the player object.");

        _characterController = GetComponent<CharacterController>();
        if (_characterController == null)
            Debug.LogWarning("PlayerController::Start(): has not found the character controller reference.");

        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;

        // initialize input array
        _inputs = new bool[5];

        // Telling the local player who the host ist (master client)
        if (player.GetPlayerType() == PlayerType.MasterClient) {
            LocalServerManager.Instance.masterClient = this;
        }
    }

    private void FixedUpdate() {
        /*
         * INFO: For lan just moving the player like the host.
         * Sending all data directly to the players, without calculating movement first.
         * I do not have the interfaces set for this kind of behaviour and there is still
         * need to figure out what whould be the best behaviour for this.
         * So TODO: Creating master client check for the local client!!
         * And TODO: Split CalculatePlayerMovement() so having client and host func.
         */
        switch (player.GetPlayerType()) {
            // "Normal" player: Collecting movement and sending input to server.
            // In LAN: Moving and sending position and rotation updates to everyone.
            case PlayerType.Player:
                bool _gameHavingMasterClient = false;
                GatherPlayerInputs();

                if (_gameHavingMasterClient) {
                    // TODO: Figure out how to use this in LAN.
                    //CalculatePlayerMovement();
                    SendPositionToServer();
                } else {
                    SendInputToServer();
                }
                break;
            // This is the absolute master client (host)
            // Collecting data and seinding it to all clients.
            case PlayerType.MasterClient:
                GatherPlayerInputs();
                CalculatePlayerMovement();
                break;
            // Clone should move on server side.
            // Calculate PlayerClones values and send them to everyone.
            default:
                CalculatePlayerMovement();
                break;
        }
    }

    // Client: get input and send to server
    // Server: receive input and calculate player reference movement
    // MasterClient: Ownly it's own movement.
    public void GatherPlayerInputs() {
        _inputs = new bool[] {
            Input.GetKey(KeyCode.W),
            Input.GetKey(KeyCode.S),
            Input.GetKey(KeyCode.A),
            Input.GetKey(KeyCode.D),
            Input.GetKey(KeyCode.Space)
        };
    }

    // client => server
    public void SendInputToServer() {
        // For debugging purposes.
        // Debug.Log($"PlayerController::SendInputToServer(): Player '{player.GetPlayerID()}:{player.GetUsername()}' sending input to server...");

        ClientSend.PlayerMovement(_inputs);
    }

    // CLIENT-SIDE
    // INFO: Only in LAN: Sending PlayerMovement directly to server.
    // This is for having the server to relay it directly to the clients.
    public void SendPositionToServer() {
        ClientSend.PlayerMovement(transform.position, transform.rotation);
    }

    // SERVER-SIDE
    // INFO: Only in LAN: Receiving PlayerMovement directly from client.
    // Redirecting movement back to client...
    public void ReceivePositionData(Vector3 _position, Quaternion _rotation) {
        LocalServerSend.PlayerPosition(player.GetPlayerID(), _position);
        LocalServerSend.PlayerRotation(player.GetPlayerID(), _rotation);
    }

    private void CalculatePlayerMovement() {
        // Having this check just for when the input whould be empty.
        if (_inputs == null || _inputs.Length == 0) {
            Debug.LogError("PlayerController::CalculatePlayerController(): Input array is empty or null");
            return;
        }

        Vector2 _inputDirection = Vector2.zero;

        // Catching inputs.
        if (_inputs[0]) {
            _inputDirection.y += 1;
        }
        if (_inputs[1]) {
            _inputDirection.y -= 1;
        }
        if (_inputs[2]) {
            _inputDirection.x -= 1;
        }
        if (_inputs[3]) {
            _inputDirection.x += 1;
        }

        MovePlayer(ref _inputDirection);
    }

    private void MovePlayer(ref Vector2 _inputDirection) {
        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        _moveDirection *= moveSpeed;

        if (_characterController.isGrounded) {
            _yVelocity = 0F;

            // check if player jumped.
            if (_inputs[4]) {
                _yVelocity = jumpSpeed;
            }
        }
        _yVelocity += gravity;

        _moveDirection.y = _yVelocity;
        _characterController.Move(_moveDirection);

        // Send position to everyone, the rotation to everyone except the player it belongs to...
        // Both working, having a overload function.
        //ServerSend.PlayerPosition(player.GetPlayerID(), transform.position);
        //ServerSend.PlayerRotation(player.GetPlayerID(), transform.rotation);
        LocalServerSend.PlayerPosition(player);
        LocalServerSend.PlayerRotation(player);
    }

    // called on the server for calulcating player movement for the player reference.
    public void SetPlayerInput(bool[] _inputs, Quaternion _rotation) {
        // Debugging player input...
        //Debug.Log($"PlayerController::SetPlayerInput(): called for playerid '{player.GetPlayerID()}'.");

        // There should never be an empty input array.
        if (_inputs == null) {
            Debug.LogWarning($"PlayerController::SetPlayerInput(): Received empty input on player with id {player.GetPlayerID()}");
            return;
        }

        // Should not be reached, as this is a human error...
        if (this._inputs.Length != _inputs.Length)
            Debug.LogError($"PlayerController::SetPlayerInput(): input array length mismatch for player with id {player.GetPlayerID()}.", this);

        this._inputs = _inputs;
        transform.rotation = _rotation;
    }
}