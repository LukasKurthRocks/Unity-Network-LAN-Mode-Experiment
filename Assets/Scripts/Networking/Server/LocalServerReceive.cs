﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/*
 * TODO: Just sending the input data back to client?
 * Maybe even having everyone on same Player.MasterClient logic?
 * Creating another "PlayerMovement" class with Vector parameters?
 */

public class LocalServerReceive {
    #region LAN Packets
    public static void Ping(string _remoteEndPoint, Packet _packet) {
        Debug.Log($"LocalServerReceive::Ping(): Ping received from '{_remoteEndPoint}'. Sending pong ...");

        if (_packet == null)
            Debug.Log("Received empty ping packet.");

        LocalServerSend.SendPong();
    }
    #endregion

    #region Standard Packets
    public static void WelcomeReceived(int _fromClient, Packet _packet) {
        // Just checking for empty packets. Should not happen, happened once!
        if (_packet == null) {
            Debug.LogError("ServerReceive::WelcomeReceived(): Welcome receive packet is null.");
            return;
        }

        int _clientIdCheck = _packet.ReadInt();
        string _username = _packet.ReadString();

        Debug.Log($"ServerReceive::WelcomeReceived(): {LocalServer.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
        if (_fromClient != _clientIdCheck) {
            Debug.Log($"ServerReceive::WelcomeReceived(): Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
        }

        // Send player into game
        if (LocalServer.clients[_fromClient] == null) {
            Debug.LogError("ServerReceive::WelcomeReceived(): Trying to send player into game. Client itself is null though...");
        }
        LocalServer.clients[_fromClient].SendIntoGame(_username);
    }

    // Local Server should just receive Client input, not calculating it...
    public static void PlayerMovement(int _fromClient, Packet _packet) {
        bool[] _inputs = new bool[_packet.ReadInt()];
        for (int i = 0; i < _inputs.Length; i++) {
            _inputs[i] = _packet.ReadBool();
        }

        Quaternion _rotation = _packet.ReadQuarternion();

        // Setting player input when received, so server side player clone moves along.
        LocalServer.clients[_fromClient].player.GetComponent<PlayerController>().SetPlayerInput(_inputs, _rotation);
    }

    // INFO: Only in LAN: Sending PlayerMovement directly to server.
    // This is for having the server to relay it directly to the clients.
    public static void LANPlayerMovement(int _fromClient, Packet _packet) {
        Vector3 _position = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuarternion();

        // Server receiving client position and redirecting it back to every client.
        LocalServer.clients[_fromClient].player.GetComponent<PlayerController>().ReceivePositionData(_position, _rotation);
    }
    #endregion
}