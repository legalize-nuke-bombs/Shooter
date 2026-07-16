package com.example.shooter.game.player;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;

public interface PlayerRepository extends JpaRepository<Player, Long> {
    @Query("SELECT p FROM Player p JOIN FETCH p.world WHERE p.user.id = ?1 AND p.role >= ?2")
    List<Player> findAllByUserIdAndPlayerRoleWithWorlds(Long userId, PlayerRole playerRole);

    @Query("SELECT p FROM Player p JOIN FETCH p.world JOIN FETCH p.user WHERE p.world.id in ?1")
    List<Player> findAllByWorldIdsWithWorldsAndUsers(Set<UUID> worldIds);

    @Query("SELECT p FROM Player p WHERE p.user.id = ?1 AND p.world.id = ?2")
    Optional<Player> findByUserIdAndWorldId(Long userId, UUID worldId);

    @Modifying
    @Query("DELETE FROM Player p WHERE p.user.id = ?1 AND p.world.id = ?2")
    long deleteByUserIdAndWorldId(Long userId, UUID worldId);

    @Query("SELECT p FROM Player p WHERE p.world.id = ?1")
    List<Player> findAllByWorldId(UUID worldId);
}
