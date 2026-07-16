package com.example.shooter.game.world;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

@Entity
@Table(name = "worlds")
@Getter
@Setter
public class World {
    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;

    @Column(nullable = false, length = WorldConstants.MAX_NAME_LENGTH)
    private String name;

    @Column(nullable = false)
    private Long createdAt;

    @Column(nullable = false)
    private Long accessedAt;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private WorldJoinPolicy joinPolicy;

    @Column(nullable = false)
    private Long players;
}
