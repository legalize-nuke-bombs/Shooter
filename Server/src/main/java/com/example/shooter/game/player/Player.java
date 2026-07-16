package com.example.shooter.game.player;

import com.example.shooter.game.world.World;
import com.example.shooter.user.User;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.Setter;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;

@Entity
@Table(name = "players", indexes = {
        @Index(name = "idx_players_world_id", columnList = "world_id"),
        @Index(name = "idx_players_user_id", columnList = "user_id"),
        @Index(name = "idx_players_last_seen", columnList = "last_seen"),
}, uniqueConstraints = {
        @UniqueConstraint(columnNames = {"world_id", "user_id"})
})
@Getter
@Setter
public class Player {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne
    @JoinColumn(name = "world_id", nullable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private World world;

    @ManyToOne
    @JoinColumn(name = "user_id", nullable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private User user;

    @Column(nullable = false)
    private PlayerRole role;

    @Column(nullable = false)
    private Long memberSince;

    @Column(nullable = false)
    private Long lastSeen;
}
