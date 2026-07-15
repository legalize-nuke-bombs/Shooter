package com.example.shooter.game.world;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

@Entity
@Table(name = "worlds", indexes = {
        @Index(name = "idx_worlds_accessed_at", columnList = "accessed_at"),
        @Index(name = "idx_worlds_visibility_policy", columnList = "visibility_policy"),
        @Index(name = "idx_worlds_join_policy", columnList = "join_policy")
})
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
    private WorldVisibilityPolicy visibilityPolicy;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private WorldJoinPolicy joinPolicy;

    @Column(nullable = false, length = WorldConstants.MAX_DESCRIPTION_LENGTH)
    private String description;
}
