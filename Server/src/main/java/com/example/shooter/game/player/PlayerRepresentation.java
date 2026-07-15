package com.example.shooter.game.player;

import com.example.shooter.game.util.Vector3d;
import com.example.shooter.user.UserRepresentation;
import lombok.Getter;

@Getter
public class PlayerRepresentation {
    private final Long id;
    private final UserRepresentation user;
    private final PlayerRole role;
    private final Long memberSince;
    private final Vector3d position;

    public PlayerRepresentation(Player player) {
        this.id = player.getId();
        this.user = new UserRepresentation(player.getUser());
        this.role = player.getRole();
        this.memberSince = player.getMemberSince();
        this.position = player.getPosition();
    }
}
