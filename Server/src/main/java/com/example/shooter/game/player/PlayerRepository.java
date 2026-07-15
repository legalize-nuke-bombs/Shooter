package com.example.shooter.game.player;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.Set;
import java.util.UUID;

public interface PlayerRepository extends JpaRepository<Player, Long> {
    @Query("SELECT p FROM Player p JOIN FETCH p.world WHERE p.user.id = ?1 AND p.role = ?2")
    List<Player> findAllByUserIdAndRoleWithWorlds(Long userId, PlayerRole role);

    @Query("SELECT p FROM Player p JOIN FETCH p.world WHERE p.world.id in ?1")
    List<Player> findAllByWorldIdsWithWorlds(Set<UUID> worldIds);
}
