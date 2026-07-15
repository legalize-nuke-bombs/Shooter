package com.example.shooter.world;

import com.example.shooter.user.User;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.Setter;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;

@Entity
@Table(name = "worlds", indexes = {
        @Index(name = "idx_worlds_name", columnList = "name"),
        @Index(name = "idx_worlds_created_at", columnList = "created_at"),
        @Index(name = "idx_worlds_creator_id", columnList = "creator_id")
})
@Getter
@Setter
public class World {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, length = WorldConstants.MAX_NAME_LENGTH)
    private String name;

    @Column(nullable = false)
    private Long createdAt;

    @ManyToOne
    @JoinColumn(name = "creator_id", nullable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private User creator;

    @Column(nullable = false, length = WorldConstants.MAX_DESCRIPTION_LENGTH)
    private String description;
}
