package com.example.shooter.game.world;

import com.example.shooter.game.player.PlayerRepresentation;
import lombok.Getter;

import java.util.List;
import java.util.UUID;

@Getter
public class WorldRepresentation {
    private final UUID id;
    private final String name;
    private final Long createdAt;
    private final WorldJoinPolicy joinPolicy;
    private final List<PlayerRepresentation> players;

    public WorldRepresentation(World world, List<PlayerRepresentation> players) {
        this.id = world.getId();
        this.name = world.getName();
        this.createdAt = world.getCreatedAt();
        this.joinPolicy = world.getJoinPolicy();
        this.players = players;
    }

}
